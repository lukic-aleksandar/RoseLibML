using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using System;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoseLibML.Util;
using RoseLibML.CS.CSTrees;

namespace RoseLibML.LanguageServer
{
    internal class CommandHandler : IExecuteCommandHandler<CommandResponse>
    {

        private const string pCFGCommand = "rose-lib-ml.runPCFG";
        private const string MCMCCommand = "rose-lib-ml.runMCMC";
        private const string generateCommand = "rose-lib-ml.generate";

        private ExecuteCommandCapability _capability;

        public Task<CommandResponse> Handle(ExecuteCommandParams<CommandResponse> request, CancellationToken cancellationToken)
        {

            JObject requestArguments = (JObject)request.Arguments.First();

            Task<CommandResponse> response;

            switch (request.Command)
            {
                case pCFGCommand:

                    Log.Logger.Debug("CommandHandler | pCFG command");

                    PCFGCommandArguments pCFGArguments = requestArguments.ToObject<PCFGCommandArguments>();

                    if (!Directory.Exists(pCFGArguments.InputFolder))
                    {
                        response = Task.FromResult(new CommandResponse($"Input folder on path '{pCFGArguments.InputFolder}' doesn't exist.", true));

                        break;
                    }
                    else if (!Directory.Exists(Path.GetDirectoryName(pCFGArguments.OutputFile)))
                    {
                        response = Task.FromResult(new CommandResponse($"Directory of the output file on path {Path.GetDirectoryName(pCFGArguments.OutputFile)} doesn't exist.", true));

                        break;
                    }
                    else if (pCFGArguments.ProbabilityCoefficient < 0 || pCFGArguments.ProbabilityCoefficient > 1)
                    {
                        response = Task.FromResult(new CommandResponse("Probability coefficient must be a number between 0 and 1.", true));

                        break;
                    }

                    try
                    {
                        Dictionary<string, double> probabilities = CalculateAndSavePCFG(pCFGArguments);

                        Log.Logger.Debug("CommandHandler | pCFG command | Succesfully done ");
                        response = Task.FromResult(new CommandResponse(probabilities, "Succesfully done.", false));
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error("CommandHandler | pCFG command | " + e.Message);
                        response = Task.FromResult(new CommandResponse(e.Message, true)); ;
                    }

                    break;
                case MCMCCommand:

                    Log.Logger.Debug("CommandHandler | MCMC command");

                    MCMCCommandArguments MCMCarguments = requestArguments.ToObject<MCMCCommandArguments>();

                    if (MCMCarguments.Iterations <= 0 || MCMCarguments.BurnInIterations < 0)
                    {
                        response = Task.FromResult(new CommandResponse("The number of iterations and burn in iterations must be positive.", true));

                        break;
                    }
                    else if (MCMCarguments.InitialCutProbability < 0 || MCMCarguments.InitialCutProbability > 1)
                    {
                        response = Task.FromResult(new CommandResponse("Cut probability must be a number between 0 and 1.", true));

                        break;
                    }
                    else if (!Directory.Exists(MCMCarguments.InputFolder))
                    {
                        response = Task.FromResult(new CommandResponse($"Input folder on path '{MCMCarguments.InputFolder}' doesn't exist.", true));

                        break;
                    }
                    else if (!File.Exists(MCMCarguments.PCFGFile))
                    {
                        response = Task.FromResult(new CommandResponse($"pCFG file on path '{MCMCarguments.PCFGFile}' doesn't exist.", true));

                        break;
                    }
                    else if (!Directory.Exists(MCMCarguments.OutputFolder))
                    {
                        response = Task.FromResult(new CommandResponse($"Output folder on path '{MCMCarguments.OutputFolder}' doesn't exist.", true));

                        break;
                    }

                    try
                    {
                        Dictionary<int, List<string>> fragments = InitializeAndTrain(MCMCarguments);

                        Log.Logger.Debug("CommandHandler | MCMC command | Succesfully done ");
                        response = Task.FromResult(new CommandResponse(fragments, "Succesfully done.", false));
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error("CommandHandler | MCMC command | " + e.Message);
                        response = Task.FromResult(new CommandResponse(e.Message, true)); ;
                    }

                    break;
                case generateCommand:
                    response = Task.FromResult(new CommandResponse("Not implemented yet.", true));

                    break;
                default:
                    response = Task.FromResult(new CommandResponse("Unknown command", true));

                    break;
            }

            return response;
        }

        public void SetCapability(ExecuteCommandCapability capability)
        {
            _capability = capability;
        }

        public ExecuteCommandRegistrationOptions GetRegistrationOptions(ExecuteCommandCapability capability, ClientCapabilities clientCapabilities)
        {
            return new ExecuteCommandRegistrationOptions()
            {
                Commands = new List<string> { pCFGCommand, MCMCCommand, generateCommand },
            };
        }

        private Dictionary<string, double> CalculateAndSavePCFG(PCFGCommandArguments arguments)
        {
            // create labeled trees and perform needed transformations
            var labeledTrees = CreateLabeledTrees(arguments.InputFolder, "");

            Config config = new Config()
            {
                ModelParams = new ModelParams()
                {
                    P = arguments.ProbabilityCoefficient
                }
            };

            // calculate probabilitites and save pCFG to file
            var pCFGComposer = new LabeledTreePCFGComposer(labeledTrees.ToList(), config);

            pCFGComposer.CalculateProbabilities();
            pCFGComposer.Serialize(arguments.OutputFile);

            return pCFGComposer.GetRulesProbabilities();
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