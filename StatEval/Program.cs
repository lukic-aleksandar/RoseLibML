﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS.CSTrees;
using RoseLibML.Util;
using System;
using System.ComponentModel.DataAnnotations;

namespace StatEval
{
    // Problem mogu napraviti samo situacije u kojima stringovi sadrze zagrade.
    // Imam preprocesor, tamo bi to moglo lagano da se resi, sanitizuje. Ovde nemam taj tokenizer, pa nije bas lako.
    // To je problem koji ostavljam za posle :) 
    public class Program
    {
        static Dictionary<string, (FileInfo syntaxTreeFI, FileInfo csTreeFI)> counterpartsPaths = new Dictionary<string, (FileInfo syntaxTreeFI, FileInfo CSTreeFI)>();
        static Dictionary<string, (SyntaxTree syntaxTree, CSTree csTree)> counterpartsTrees = new Dictionary<string, (SyntaxTree syntaxTree, CSTree csTree)>();

        static List<LabeledTree> testLabeledTrees = new List<LabeledTree>();

        public static void Main(string[] args)
        {
            try
            {
                var trainingConfig = ExtractConfig(args);
                var idiomHandler = IdiomHandler.CreateEmptyIdiomHandler();

                do
                {
                    counterpartsPaths = new Dictionary<string, (FileInfo syntaxTreeFI, FileInfo CSTreeFI)>();
                    counterpartsTrees = new Dictionary<string, (SyntaxTree syntaxTree, CSTree csTree)>();

                    ReadTrainingFiles(trainingConfig.Paths!.InData!, trainingConfig.Paths!.OutModel!);


                    idiomHandler.TrainingLabeledTrees = counterpartsTrees
                                                    .Values
                                                    .Select(t => t.csTree)
                                                    .Cast<LabeledTree>()
                                                    .ToList();
                    idiomHandler.SortOutIdiomsInTrainingSet(trainingConfig.RunParams!.Threshold, trainingConfig.RunParams.IdiomLengthThreshold);

                    Console.WriteLine("Press Y to continue sampling.");
                } while (Console.ReadLine()!.ToLower().StartsWith('y'));
                Console.Clear();

                ReadTestFiles(trainingConfig, args[1]);
                Console.WriteLine($"Alpha: {trainingConfig.ModelParams!.DefaultAlpha}");
                Console.WriteLine($"Size threshold: {trainingConfig.RunParams.IdiomLengthThreshold}, count threshold: {trainingConfig.RunParams.Threshold}");
                // Not really optimized, it happens only once, at the end of the testing.
                var precision = idiomHandler.CalculatePrecision(testLabeledTrees);
                Console.WriteLine($"The precision was: {precision}");
                var coverage = idiomHandler.CalculateCoverage(testLabeledTrees, out int totalMarked);
                Console.WriteLine($"The coverage was: {coverage}");

                var avgIdiomLength = idiomHandler.CalcualteAverageIdiomLength();
                Console.WriteLine($"The average idiom length was {avgIdiomLength}");

                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadKey();
            }
        }

        public static void ReadTrainingFiles(string inDataDir, string outModelDir)
        {
            LoadCounterpartsPaths(inDataDir, outModelDir);
            LoadCounterpartsTrees();
        }

        private static void LoadCounterpartsPaths(string csFilesPath, string binFilesPath)
        {
            var csDirectoryInfo = new DirectoryInfo(csFilesPath);
            var binDirectoryInfo = new DirectoryInfo(binFilesPath);

            var csFiles = csDirectoryInfo.GetFiles("*.cs");
            var binFiles = binDirectoryInfo.GetFiles("*.bin");

            foreach (var csFileInfo in csFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(csFileInfo.Name);
                var binFileInfo = binFiles
                                    .Where(b => Path.GetFileNameWithoutExtension(b.Name) == csFileInfo.Name)
                                    .FirstOrDefault();
                if (binFileInfo == null)
                {
                    throw new DataMisalignedException("For some reason, a .cs file does not have it's binary counterpart");
                }


                (FileInfo syntaxTreeFI, FileInfo CSTreeFI) counterparts = (csFileInfo, binFileInfo);
                counterpartsPaths.Add(fileName, counterparts);
            }
        }

        private static void LoadCounterpartsTrees()
        {
            foreach (var fileName in counterpartsPaths.Keys)
            {
                using (var streamReader = new StreamReader(counterpartsPaths[fileName].syntaxTreeFI.FullName))
                {
                    var code = streamReader.ReadToEnd();
                    var syntaxTree = CSharpSyntaxTree.ParseText(code);

                    var labeledTree = CSTreeCreator.Deserialize(counterpartsPaths[fileName].syntaxTreeFI.FullName, counterpartsPaths[fileName].csTreeFI.FullName, fileName);
                    if (labeledTree == null)
                    {
                        throw new FileNotFoundException($"A problem with deserializing file {counterpartsPaths[fileName].csTreeFI}.");
                    }

                    (SyntaxTree syntaxTree, CSTree csTree) treesTuple = (syntaxTree, labeledTree);
                    counterpartsTrees.Add(fileName, treesTuple);
                }
            }
        }

        private static void ReadTestFiles(Config trainingConfig, string inDataDir)
        {
            var config = trainingConfig.Clone();
            config.Paths!.InData = inDataDir;
            config.Paths!.OutModel = $"{inDataDir}\\out\\model";

            testLabeledTrees = CreateLabeledTrees(config).ToList();
        }

        public static Config ExtractConfig(string[] args)
        {
            var validationResults = new List<ValidationResult>();
            Config config = null;

            if (args.Length == 0)
            {
                throw new Exception("A path to config file must be provided.");
            }

            try
            {
                config = ConfigReader.ReadValidateConfig(args[0], validationResults);
            }
            catch (Exception e)
            {
                throw new Exception("Could not read from json.");
            }

            if (validationResults.Count > 0)
            {
                var messages = validationResults.Select(vr => vr.ErrorMessage).ToArray();
                var unifiedMessage = string.Join("; ", messages);
                throw new Exception(unifiedMessage);
            }

            if (config != null)
            {
                var pathCheckMessages = "";
                if (config.Paths == null 
                    || config.Paths.InData == null 
                    || config.Paths.InData.Length == 0 
                    || !Directory.Exists(config.Paths.InData))
                {
                    pathCheckMessages += "A path to training cs files has to be specified in the configuration.";
                }

                if (config.Paths == null
                    || config.Paths.OutModel == null
                    || config.Paths.OutModel.Length == 0
                    || !Directory.Exists(config.Paths.OutModel))
                {
                    pathCheckMessages += " A path to training output bin files has to be specified in the configuration.";
                }

                if (pathCheckMessages.Length > 0)
                {
                    throw new Exception(pathCheckMessages);
                }
            }

            return config;
        }

        public static LabeledTree[] CreateLabeledTrees(Config config)
        {
            var inputModelPresent = !string.IsNullOrEmpty(config?.Paths?.InModel);
            var directoryInfo = new DirectoryInfo(config?.Paths?.InData);
            var files = directoryInfo.GetFiles();

            LabeledTree[] labeledTrees = new LabeledTree[files.Length];

            Parallel.For(0, files.Length, (index) =>
            {
                try
                {
                    if (!inputModelPresent)
                    {
                        var labeledTree = CSTreeCreator.CreateTree(files[index], config.Paths.OutModel, config.FixedNodeKinds);
                        if (labeledTree != null)
                        {
                            LabeledTreeTransformations.Binarize(labeledTree.Root, new CSNodeCreator(config.FixedNodeKinds), files[index].FullName);
                            labeledTrees[index] = labeledTree;
                        }
                    }
                    else
                    {
                        var labeledTree = CSTreeCreator.Deserialize(files[index], config?.Paths?.InModel, config.Paths.OutModel);
                        if (labeledTree != null)
                        {
                            labeledTrees[index] = labeledTree;
                        }
                    }
                }
                catch
                {
                    Console.WriteLine($"Was not able to create and binarize the file, or to deserialize it: {files[index].FullName}");
                }
                
            });

            return labeledTrees.Where(lt => lt != null).ToArray(); ;
        }
    }
}
