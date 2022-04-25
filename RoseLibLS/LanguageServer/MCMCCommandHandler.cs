using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Progress;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.WorkDone;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using RoseLibLS.Util;
using RoseLibML;
using RoseLibML.CS.CSTrees;
using RoseLibML.Util;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RoseLibLS.LanguageServer
{
    internal class MCMCCommandHandler : IExecuteCommandHandler<CommandResponse>
    {
        private ExecuteCommandCapability capability;
        private readonly ILanguageServerFacade server;
        private readonly IServerWorkDoneManager serverWorkDoneManager;

        public MCMCCommandHandler(ILanguageServerFacade server, IServerWorkDoneManager serverWorkDoneManager)
        {
            this.server = server;
            this.serverWorkDoneManager = serverWorkDoneManager;
        }

        public async Task<CommandResponse> Handle(ExecuteCommandParams<CommandResponse> request, CancellationToken cancellationToken)
        {
            Log.Logger.Debug("MCMC Command Handler");

            MCMCCommandArguments MCMCarguments = request.Arguments.First().ToObject<MCMCCommandArguments>();

            var workDoneReporter = await serverWorkDoneManager.Create(
                new WorkDoneProgressBegin
                {
                    Cancellable = false,
                    Message = "Started the MCMC phase",
                    Title = "MCMC Command Handler",
                    Percentage = 0
                }
            );

            ProgressListener listener = new ProgressListener(workDoneReporter, MCMCarguments.Iterations);

            List<string> errorsMCMC = Validation.ValidateArguments(MCMCarguments);
            if (errorsMCMC.Count > 0)
            {
                string validationErrors = string.Join(" ", errorsMCMC);

                Log.Logger.Error($"MCMC Command Handler | {validationErrors}", validationErrors);
                return new CommandResponse(validationErrors, true);
            }

            try
            {
                _ = Task.Run(() =>
                    {
                        Dictionary<int, List<string>> fragments = InitializeAndTrain(MCMCarguments, listener);

                        workDoneReporter.OnNext(new WorkDoneProgressEnd { Message = "Succesfully finished the MCMC phase." });
                        workDoneReporter.OnCompleted();

                        Log.Logger.Debug("MCMC Command Handler | Succesfully done.");
                        server.Window.SendNotification("window/showMessage", new CommandNotification("showMCMC", fragments, "MCMC phase succesfully done."));
                    }).ContinueWith(t =>
                        {
                            Log.Logger.Error("MCMC Command Handler | " + t.Exception.Message);

                            workDoneReporter.OnNext(new WorkDoneProgressEnd { Message = "An error occurred. Please try again." });
                            workDoneReporter.OnCompleted();
                        },
                        default,
                        TaskContinuationOptions.OnlyOnFaulted,
                        TaskScheduler.Default
                    );

                Log.Logger.Debug("MCMC Command Handler | Succesfully started");
                return new CommandResponse("MCMC phase succesfully started.", false);
            }
            catch (Exception e)
            {
                Log.Logger.Error("MCMC Command Handler | " + e.Message);
                return new CommandResponse("An error occurred. Please try again.", true);
            }
        }

        public void SetCapability(ExecuteCommandCapability capability)
        {
            capability = this.capability;
        }

        public ExecuteCommandRegistrationOptions GetRegistrationOptions(ExecuteCommandCapability capability, ClientCapabilities clientCapabilities)
        {
            return new ExecuteCommandRegistrationOptions()
            {
                Commands = new List<string> { "rose-lib-ml.runMCMC" },
            };
        }

        private Dictionary<int, List<string>> InitializeAndTrain(MCMCCommandArguments arguments, ProgressListener listener)
        {
            // create labeled trees and perform needed transformations
            var labeledTrees = CreateLabeledTrees(arguments.InputFolder, arguments.OutputFolder);

            // read pCFG from file
            LabeledTreePCFGComposer pCFGComposer = LabeledTreePCFGComposer.Deserialize(arguments.PCFGFile);

            ToCSWriter writer = new ToCSWriter(arguments.OutputFolder + $"\\idioms__{DateTime.Now.ToString("yyyyMMddHHmmss")}.txt");

            Config config = new Config()
            {
                RunParams = new RunParams()
                {
                    StartIteration = 0,
                    TotalIterations = arguments.Iterations,
                    Threshold = arguments.Threshold,
                    BurnIn = arguments.BurnInIterations,
                },
                ModelParams = new ModelParams()
                {
                    Alpha = arguments.Alpha,
                    CutProbability = arguments.InitialCutProbability,
                }
            };

            // initialize Gibbs Sampler and start training
            var sampler = new TBSampler(writer, config);

            sampler.AddListener(listener);
            sampler.Initialize(pCFGComposer, labeledTrees);

            sampler.Train();

            // serialize trees
            foreach (var tree in sampler.Trees)
            {
                tree.Serialize();
            }

            return writer.FragmentsPerIteration;
        }

        private LabeledTree[] CreateLabeledTrees(string sourceDirectory, string outputDirectory)
        {
            var directoryInfo = new DirectoryInfo(sourceDirectory);
            var files = directoryInfo.GetFiles();

            LabeledTree[] labeledTrees = new LabeledTree[files.Length];

            Parallel.For(0, files.Length, (index) =>
            {
                var labeledTree = CSTreeCreator.CreateTree(files[index], outputDirectory);
                LabeledTreeTransformations.Binarize(labeledTree.Root, new CSNodeCreator());
                labeledTrees[index] = labeledTree;
            });

            return labeledTrees;
        }
    }
}
