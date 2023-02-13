using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS;
using RoseLibML.CS.CSTrees;
using RoseLibML.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RoseLibML.Core;
using RoseLibML.Core.PCFG;

namespace RoseLibML
{
    class Program
    {
        static void Main(string[] args)
        {
            Config config = TryExtractConfig(args);
            if (config == null)
            {
                return;
            }

            var loadModel = !string.IsNullOrEmpty(config.Paths.InModel);
            var justWriteTheFragments = config.RunParams.JustWriteTheFragments;
            var saveModel = !string.IsNullOrEmpty(config.Paths.OutModel);

            // Create Labeled Trees
            // Perform Needed transformations
            var labeledTrees = CreateLabeledTrees(config);

            // Calculate and save PCFG to file
            var pCFGComposer = new LabeledTreePCFGComposer(labeledTrees.ToList(), config);
            pCFGComposer.CalculateProbabilitiesLn();


            ToCSWriter writer = new ToCSWriter(config.Paths.OutIdioms);

            var sampler = new TBSampler(writer, config);

            sampler.Initialize(pCFGComposer, labeledTrees, loadModel);

            if (!justWriteTheFragments)
            {
                sampler.Train();

                if (saveModel)
                {
                    foreach (var tree in sampler.Trees)
                    {
                        tree.Serialize();
                    }
                }
            }
            else
            {
                var threshold = config.RunParams.Threshold;
                var iteration = config.RunParams.StartIteration;
                sampler.WriteFragments(threshold, iteration);
                Console.WriteLine("Finished writing of fragments...");
            }


            Console.ReadKey();
        }

        static Config TryExtractConfig(string[] args)
        {
            var validationResults = new List<ValidationResult>();
            Config config = null;

            if (args.Length != 1)
            {
                Console.WriteLine("A path to config file must be provided.");
                Console.ReadKey();
            }

            try
            {
                config = ConfigReader.ReadValidateConfig(args[0], validationResults);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not read from json.");
                Console.ReadKey();
            }

            if (validationResults.Count > 0)
            {
                validationResults.ForEach(vr => Console.WriteLine(vr.ErrorMessage));
                Console.ReadKey();
            }

            return config;
        }

        static LabeledTree[] CreateLabeledTrees(Config config)
        {
            var inputModelPresent = !string.IsNullOrEmpty(config?.Paths?.InModel);
            var directoryInfo = new DirectoryInfo(config?.Paths?.InData);
            var files = directoryInfo.GetFiles();

            LabeledTree[] labeledTrees = new LabeledTree[files.Length];

            Parallel.For(0, files.Length, (index) =>
            //for(int index = 0; index < files.Length;index++)
            {
                if (!inputModelPresent)
                {
                    var labeledTree = CSTreeCreator.CreateTree(files[index], config.Paths.OutModel, config.FixedNodeKinds);
                    LabeledTreeTransformations.Binarize(labeledTree.Root, new CSNodeCreator(config.FixedNodeKinds));
                    labeledTrees[index] = labeledTree;
                }
                else
                {
                    var labeledTree = CSTreeCreator.Deserialize(files[index], config?.Paths?.InModel, config.Paths.OutModel);
                    if (labeledTree != null)
                    {
                        labeledTrees[index] = labeledTree;
                    }
                }
            });

            return labeledTrees.Where(lt => lt != null).ToArray(); ;
        }
    }
}
