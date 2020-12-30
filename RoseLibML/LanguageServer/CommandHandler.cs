using MediatR;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RoseLibML.LanguageServer
{
    internal class CommandHandler : IExecuteCommandHandler<CommandResponse>
    {
        private ExecuteCommandCapability _capability;

        public Task<CommandResponse> Handle(ExecuteCommandParams<CommandResponse> request, CancellationToken cancellationToken)
        {
            Task<CommandResponse> response;

            JObject requestArguments = (JObject)request.Arguments.First();

            switch (request.Command)
            {
                case "rose-lib-ml.runPCFG":
                    PCFGCommandArguments pCFGArguments = requestArguments.ToObject<PCFGCommandArguments>();

                    try
                    {
                        CalculateAndSavePCFG(pCFGArguments);

                        response = Task.FromResult(new CommandResponse("Succesfully done.", true));
                    }
                    catch (FileNotFoundException e)
                    {
                        response = Task.FromResult(new CommandResponse(e.Message, false));
                    }
                    catch (DirectoryNotFoundException e)
                    {
                        response = Task.FromResult(new CommandResponse(e.Message, false));
                    }
                    catch (IOException e)
                    {
                        response = Task.FromResult(new CommandResponse(e.Message, false));
                    }
                    catch (Exception)
                    {
                        response = Task.FromResult(new CommandResponse("An error occurred.", false)); ;
                    }

                    break;
                case "rose-lib-ml.runMCMC":
                    MCMCCommandArguments MCMCarguments = requestArguments.ToObject<MCMCCommandArguments>();

                    if (MCMCarguments.Iterations <= 0 || MCMCarguments.BurnInIterations < 0)
                    {
                        response = Task.FromResult(new CommandResponse("The number of iterations and burn in iterations must be positive.", false));

                        break;
                    }

                    try
                    {
                        InitializeAndTrain(MCMCarguments);

                        response = Task.FromResult(new CommandResponse("Succesfully done.", true));
                    }
                    catch (FileNotFoundException e)
                    {
                        response = Task.FromResult(new CommandResponse(e.Message, false));
                    }
                    catch (DirectoryNotFoundException e)
                    {
                        response = Task.FromResult(new CommandResponse(e.Message, false));
                    }
                    catch (IOException e)
                    {
                        response = Task.FromResult(new CommandResponse(e.Message, false));
                    }
                    catch(Exception)
                    {
                        response = Task.FromResult(new CommandResponse("An error occurred.", false)); ;
                    }

                    break;
                case "rose-lib-ml.generate":
                    response = Task.FromResult(new CommandResponse("Not implemented yet.", false));

                    break;
                default:
                    response = Task.FromResult(new CommandResponse("Unknown command", false));

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
                Commands = new List<string> { "rose-lib-ml.runPCFG", "rose-lib-ml.runMCMC", "rose-lib-ml.generate" },
            };
        }

        private void CalculateAndSavePCFG(PCFGCommandArguments arguments)
        {
            // create labeled trees and perform needed transformations
            var labeledTrees = CreateLabeledTrees(arguments.InputFolder, "");

            // calculate probabilitites and save pCFG to file
            var pCFGComposer = new LabeledTreePCFGComposer(labeledTrees.ToList())
            {
                P = arguments.ProbabilityCoefficient
            };

            pCFGComposer.CalculateProbabilities();
            pCFGComposer.Serialize(arguments.OutputFile);
        }

        private void InitializeAndTrain(MCMCCommandArguments arguments)
        {
            // create labeled trees and perform needed transformations
            var labeledTrees = CreateLabeledTrees(arguments.InputFolder, arguments.OutputFolder);

            // read pCFG from file
            LabeledTreePCFGComposer pCFGComposer = LabeledTreePCFGComposer.Deserialize(arguments.PCFGFile);

            // initialize Gibbs Sampler and start training
            var sampler = new GibbsSampler()
            {
                Alpha = arguments.Alpha,
                CutProbability = arguments.InitialCutProbability
            };

            sampler.Initialize(pCFGComposer, labeledTrees);
            sampler.Train(arguments.Iterations);

            // serialize trees
            foreach (var tree in sampler.Trees)
            {
                tree.Serialize();
            }
        }

        private LabeledTree[] CreateLabeledTrees(string sourceDirectory, string outputDirectory)
        {
            var directoryInfo = new DirectoryInfo(sourceDirectory);
            var files = directoryInfo.GetFiles();

            LabeledTree[] labeledTrees = new LabeledTree[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                var labeledTree = LabeledTree.CreateLabeledTree(files[i], outputDirectory);
                LabeledTreeTransformations.Binarize(labeledTree.Root);
                labeledTrees[i] = labeledTree;
            }

            return labeledTrees;
        }

    }
}