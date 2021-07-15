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
using Newtonsoft.Json;
using RoseLibML.LanguageServer.Transformer;

namespace RoseLibML.LanguageServer
{
    internal class CommandHandler : IExecuteCommandHandler<CommandResponse>
    {
        private const string pCFGCommand = "rose-lib-ml.runPCFG";
        private const string MCMCCommand = "rose-lib-ml.runMCMC";
        private const string idiomsCommand = "rose-lib-ml.getIdioms";
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

                    List<string> errorsPCFG = Validation.ValidateArguments(pCFGArguments);
                    if (errorsPCFG.Count > 0)
                    {
                        string validationErrors = string.Join(". ", errorsPCFG);

                        Log.Logger.Error($"CommandHandler | pCFG command | {validationErrors}", validationErrors);
                        response = Task.FromResult(new CommandResponse(validationErrors, true));
                        break;
                    }

                    try
                    {
                        Dictionary<string, double> probabilities = CalculateAndSavePCFG(pCFGArguments);

                        Log.Logger.Debug("CommandHandler | pCFG command | Succesfully done");
                        response = Task.FromResult(new CommandResponse(probabilities, "Succesfully done.", false));
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error("CommandHandler | pCFG command | " + e.Message);
                        response = Task.FromResult(new CommandResponse("An error occurred. Please try again.", true)); ;
                    }

                    break;
                case MCMCCommand:
                    Log.Logger.Debug("CommandHandler | MCMC command");

                    MCMCCommandArguments MCMCarguments = requestArguments.ToObject<MCMCCommandArguments>();

                    List<string> errorsMCMC = Validation.ValidateArguments(MCMCarguments);
                    if (errorsMCMC.Count > 0)
                    {
                        string validationErrors = string.Join(". ", errorsMCMC);

                        Log.Logger.Error($"CommandHandler | MCMC command | {validationErrors}", validationErrors);
                        response = Task.FromResult(new CommandResponse(validationErrors, true));
                        break;
                    }

                    try
                    {
                        Dictionary<int, List<string>> fragments = InitializeAndTrain(MCMCarguments);

                        Log.Logger.Debug("CommandHandler | MCMC command | Succesfully done");
                        response = Task.FromResult(new CommandResponse(fragments, "Succesfully done.", false));
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error("CommandHandler | MCMC command | " + e.Message);
                        response = Task.FromResult(new CommandResponse("An error occurred. Please try again.", true)); ;
                    }

                    break;
                case idiomsCommand:
                    Log.Logger.Debug("CommandHandler | Idioms command");

                    GetIdiomsCommandArguments getIdiomsArguments = requestArguments.ToObject<GetIdiomsCommandArguments>();

                    try
                    {
                        List<CodeIdiom> idioms = LoadCodeIdioms(getIdiomsArguments.RootType);

                        Log.Logger.Debug("CommandHandler | Idioms command | Succesfully done");
                        response = Task.FromResult(new CommandResponse(idioms, "Succesfully done.", false));
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error("CommandHandler | Idioms command | " + e.Message);
                        response = Task.FromResult(new CommandResponse("An error occurred. Please try again.", true)); ;
                    }

                    break;
                case generateCommand:
                    Log.Logger.Debug("CommandHandler | Generate command");

                    GenerateCommandArguments generateArguments = requestArguments.ToObject<GenerateCommandArguments>();

                    List<string> errorsGenerate = Validation.ValidateArguments(generateArguments);
                    if (errorsGenerate.Count > 0)
                    {
                        string validationErrors = string.Join(". ", errorsGenerate);

                        Log.Logger.Error($"CommandHandler | Generate command | {validationErrors}", validationErrors);
                        response = Task.FromResult(new CommandResponse(validationErrors, true));
                        break;
                    }

                    try
                    {
                        List<string> outputSnippets = GenerateMethodsFromIdiom(generateArguments);

                        Log.Logger.Debug("CommandHandler | Generate command | Succesfully done");
                        response = Task.FromResult(new CommandResponse(outputSnippets, "Succesfully done.", false));
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error("CommandHandler | Generate command | " + e.Message);
                        response = Task.FromResult(new CommandResponse("An error occurred. Please try again.", true)); ;
                    }

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
                Commands = new List<string> { pCFGCommand, MCMCCommand, idiomsCommand, generateCommand },
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

        private List<CodeIdiom> LoadCodeIdioms(string rootType)
        {
            List<CodeIdiom> idioms = new List<CodeIdiom>();

            using (StreamReader file = File.OpenText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"idioms_proposal.json")))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                idioms = ((JArray)JToken.ReadFrom(reader)).ToObject<List<CodeIdiom>>();

                // filter by root node type
                if (rootType != null)
                {
                    var filteredIdioms = idioms.Where(x => x.RootCSType.ToLower().Contains(rootType.ToLower()));
                    idioms = filteredIdioms.ToList();
                }

                foreach (var idiom in idioms)
                {
                    idiom.FindMetavariablesInFragment();
                }
            }

            return idioms;
        }

        private List<string> GenerateMethodsFromIdiom(GenerateCommandArguments arguments)
        {
            // load knowledge base
            using (StreamReader file = File.OpenText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"knowledge_base.json")))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                KnowledgeBase knowledgeBase = (JToken.ReadFrom(reader)).ToObject<KnowledgeBase>();

                Transformer.Transformer transformer = new Transformer.Transformer(knowledgeBase);
                return transformer.Generate(arguments);
            }
        }
    }
}