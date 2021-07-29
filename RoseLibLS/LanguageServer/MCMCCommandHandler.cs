using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
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
        private ExecuteCommandCapability _capability;

        public Task<CommandResponse> Handle(ExecuteCommandParams<CommandResponse> request, CancellationToken cancellationToken)
        {
            Log.Logger.Debug("MCMC Command Handler");

            MCMCCommandArguments MCMCarguments = request.Arguments.First().ToObject<MCMCCommandArguments>();

            List<string> errorsMCMC = Validation.ValidateArguments(MCMCarguments);
            if (errorsMCMC.Count > 0)
            {
                string validationErrors = string.Join(". ", errorsMCMC);

                Log.Logger.Error($"MCMC Command Handler | {validationErrors}", validationErrors);
                return Task.FromResult(new CommandResponse(validationErrors, true));
            }

            try
            {
                Dictionary<int, List<string>> fragments = InitializeAndTrain(MCMCarguments);

                Log.Logger.Debug("MCMC Command Handler | Succesfully done");
                return Task.FromResult(new CommandResponse(fragments, "Succesfully done.", false));
            }
            catch (Exception e)
            {
                Log.Logger.Error("MCMC Command Handler | " + e.Message);
                return Task.FromResult(new CommandResponse("An error occurred. Please try again.", true)); ;
            }

        }

        public void SetCapability(ExecuteCommandCapability capability)
        {
            _capability = capability;
        }

        public ExecuteCommandRegistrationOptions GetRegistrationOptions(ExecuteCommandCapability capability, ClientCapabilities clientCapabilities)
        {
            return new ExecuteCommandRegistrationOptions()
            {
                Commands = new List<string> { "rose-lib-ml.runMCMC" },
            };
        }

        private Dictionary<int, List<string>> InitializeAndTrain(MCMCCommandArguments arguments)
        {
            // create labeled trees and perform needed transformations
            var labeledTrees = CreateLabeledTrees(arguments.InputFolder, arguments.OutputFolder);

            // read pCFG from file
            LabeledTreePCFGComposer pCFGComposer = LabeledTreePCFGComposer.Deserialize(arguments.PCFGFile);

            ToCSWriter writer = new ToCSWriter(arguments.OutputFolder + @"\idioms.txt");

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
