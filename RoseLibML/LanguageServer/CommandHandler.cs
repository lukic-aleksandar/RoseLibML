using MediatR;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RoseLibML.LanguageServer
{
    internal class CommandHandler : IExecuteCommandHandler
    {
        private readonly OmniSharp.Extensions.LanguageServer.Protocol.Server.ILanguageServer _router;
        private ExecuteCommandCapability _capability;

        public CommandHandler(OmniSharp.Extensions.LanguageServer.Protocol.Server.ILanguageServer router)
        {
            _router = router;
        }

        public Task<Unit> Handle(ExecuteCommandParams request, CancellationToken cancellationToken)
        {
            JObject requestArguments = (JObject)request.Arguments.First();

            switch (request.Command)
            {
                case "rose-lib-ml.runPCFG":
                    PCFGCommandArguments pCFGArguments = requestArguments.ToObject<PCFGCommandArguments>();

                    if (Directory.Exists(pCFGArguments.InputFolder) && Directory.Exists(Path.GetDirectoryName(pCFGArguments.OutputFile)))
                    {
                        CalculateAndSavePCFG(pCFGArguments);
                    }

                    break;
                case "rose-lib-ml.runMCMC":
                    MCMCCommandArguments MCMCarguments = requestArguments.ToObject<MCMCCommandArguments>();

                    if (Directory.Exists(MCMCarguments.InputFolder) && Directory.Exists(MCMCarguments.OutputFolder) && File.Exists(MCMCarguments.PCFGFile) && MCMCarguments.Iterations > 0 && MCMCarguments.BurnInIterations >= 0)
                    {
                        InitializeAndTrain(MCMCarguments);
                    }

                    break;
                case "rose-lib-ml.generate":
                    //TODO: generate
                    
                    break;
                default:

                    break;
            }

            return Unit.Task;
        }

        public void SetCapability(ExecuteCommandCapability capability)
        {
            _capability = capability;
        }

        ExecuteCommandRegistrationOptions IRegistration<ExecuteCommandRegistrationOptions>.GetRegistrationOptions()
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