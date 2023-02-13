using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using RoseLibLS.LanguageServer.DTOs.Arguments;
using RoseLibLS.LanguageServer.DTOs.pCFG;
using RoseLibLS.Util;
using RoseLibML;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.Core.PCFG;
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
        private const int MAX_FRAME_SIZE = 100;
        private List<pCFGRule> pCFGRules { get; set; }
        
        public Task<CommandResponse> Handle(ExecuteCommandParams<CommandResponse> request, CancellationToken cancellationToken)
        {
            switch (request.Command)
            {
                case "rose-lib-ml.runPCFGCalculation":
                    return RunPCFGCalculationAsync(request);
                case "rose-lib-ml.loadPCFGRules":
                    return LoadPCFGRulesAsync(request);
                default:
                    return Task.FromResult(new CommandResponse("An error occurred. Please try again.", true)); ;
            }
        }

        private Task<CommandResponse> RunPCFGCalculationAsync(ExecuteCommandParams<CommandResponse> request)
        {
            Log.Logger.Debug("Run pCFG Calculation Command Handler");

            PCFGCalculationCommandArguments arguments = request.Arguments.First().ToObject<PCFGCalculationCommandArguments>();

            List<string> errorsPCFG = Validation.ValidateArguments(arguments);
            if (errorsPCFG.Count > 0)
            {
                string validationErrors = string.Join(" ", errorsPCFG);

                Log.Logger.Error($"pCFG Command Handler | {validationErrors}", validationErrors);
                return Task.FromResult(new CommandResponse(validationErrors, true));
            }

            try
            {
                SortedDictionary<string, double> probabilities = CalculateAndSavePCFG(arguments);

                pCFGRules = new List<pCFGRule>(probabilities.Count);
                foreach(var item in probabilities)
                {
                    var newPCFGRule = new pCFGRule();
                    newPCFGRule.Rule = item.Key;
                    newPCFGRule.Probability = item.Value;
                    pCFGRules.Add(newPCFGRule);
                }

                Log.Logger.Debug("pCFG Command Handler | pCFG phase succesfully done");
                
                var trueFrameSize = Math.Min(MAX_FRAME_SIZE, pCFGRules.Count);

                var frame = new Frame<pCFGRule>();
                frame.FrameNumber = 0;
                frame.Items = pCFGRules.GetRange(0, trueFrameSize);
                frame.TotalRules = pCFGRules.Count;
                frame.TotalRuleFrames = (int) Math.Ceiling((double)frame.TotalRules / MAX_FRAME_SIZE);

                return Task.FromResult(new CommandResponse(frame, "pCFG calculation succesfully done.", false));
            }
            catch (Exception e)
            {
                Log.Logger.Error("pCFG Command Handler | " + e.Message);
                return Task.FromResult(new CommandResponse("An error occurred. Please try again.", true)); ;
            }
        }

        private Task<CommandResponse> LoadPCFGRulesAsync(ExecuteCommandParams<CommandResponse> request)
        {
            Log.Logger.Debug("Load More pCFG Rules Command Handler");

            PCFGLoadingCommandArguments arguments = request.Arguments.First().ToObject<PCFGLoadingCommandArguments>();
            List<string> errorsPCFG = Validation.ValidateArguments(arguments);
            if (errorsPCFG.Count > 0)
            {
                string validationErrors = string.Join(" ", errorsPCFG);

                Log.Logger.Error($"pCFG Command Handler | {validationErrors}", validationErrors);
                return Task.FromResult(new CommandResponse(validationErrors, true));
            }
            
            if(pCFGRules == null || pCFGRules.Count == 0)
            {
                Log.Logger.Error($"pCFG Command Handler | Loading before calculation");
                return Task.FromResult(new CommandResponse("Loading before calculation", true));
            }

            var ruleStartsWith = arguments.RuleStartsWith ?? "";
            var suitableRules = pCFGRules.Where(rule => rule.Rule.StartsWith(value: ruleStartsWith, ignoreCase: true, culture: null)); ;
            if((arguments.NeededFrame * MAX_FRAME_SIZE) > suitableRules.Count())
            {
                Log.Logger.Error($"pCFG Command Handler | Loading more than there is");
                return Task.FromResult(new CommandResponse("Loading more than there is", true));
            }

            List<pCFGRule> sublist = suitableRules.Skip(arguments.NeededFrame*MAX_FRAME_SIZE).Take(MAX_FRAME_SIZE).ToList();
            var frame = new Frame<pCFGRule>();
            frame.FrameNumber = arguments.NeededFrame;
            frame.Items = sublist;
            frame.TotalRules = suitableRules.Count();
            frame.TotalRuleFrames = (int)Math.Ceiling((double)frame.TotalRules / MAX_FRAME_SIZE);


            return Task.FromResult(new CommandResponse(frame, "pCFG loading succesfully done.", false));
        }

        public void SetCapability(ExecuteCommandCapability capability)
        {
            capability = null;
        }

        public ExecuteCommandRegistrationOptions GetRegistrationOptions(ExecuteCommandCapability capability, ClientCapabilities clientCapabilities)
        {
            return new ExecuteCommandRegistrationOptions()
            {
                Commands = new List<string> { "rose-lib-ml.runPCFGCalculation", "rose-lib-ml.loadPCFGRules" },
            };
        }

        private SortedDictionary<string, double> CalculateAndSavePCFG(PCFGCalculationCommandArguments arguments)
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

            pCFGComposer.CalculateProbabilitiesLn();
            pCFGComposer.Serialize($"{arguments.OutputFolder}\\pcfg_{DateTime.Now.ToString("yyyyMMddHHmmss")}.bin");

            return null;//pCFGComposer.GetRulesProbabilities();
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
