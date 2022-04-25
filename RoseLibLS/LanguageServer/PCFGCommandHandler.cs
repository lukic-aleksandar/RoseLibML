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
    internal class PCFGCommandHandler : IExecuteCommandHandler<CommandResponse>
    {
        private ExecuteCommandCapability capability;

        public Task<CommandResponse> Handle(ExecuteCommandParams<CommandResponse> request, CancellationToken cancellationToken)
        {
            Log.Logger.Debug("pCFG Command Handler");

            PCFGCommandArguments arguments = request.Arguments.First().ToObject<PCFGCommandArguments>();

            List<string> errorsPCFG = Validation.ValidateArguments(arguments);
            if (errorsPCFG.Count > 0)
            {
                string validationErrors = string.Join(" ", errorsPCFG);

                Log.Logger.Error($"pCFG Command Handler | {validationErrors}", validationErrors);
                return Task.FromResult(new CommandResponse(validationErrors, true));
            }

            try
            {
                Dictionary<string, double> probabilities = CalculateAndSavePCFG(arguments);

                Log.Logger.Debug("pCFG Command Handler | pCFG phase succesfully done");
                return Task.FromResult(new CommandResponse(probabilities, "pCFG phase succesfully done.", false));
            }
            catch (Exception e)
            {
                Log.Logger.Error("pCFG Command Handler | " + e.Message);
                return Task.FromResult(new CommandResponse("An error occurred. Please try again.", true)); ;
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
                Commands = new List<string> { "rose-lib-ml.runPCFG" },
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
            pCFGComposer.Serialize($"{arguments.OutputFolder}\\pcfg_{DateTime.Now.ToString("yyyyMMddHHmmss")}.bin");

            return pCFGComposer.GetRulesProbabilities();
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
