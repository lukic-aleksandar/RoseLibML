using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoseLibML;
using RoseLibML.CS.CSTrees;
using RoseLibML.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLib
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

            var saveModel = !string.IsNullOrEmpty(config.Paths.OutModel);

            // Create Labeled Trees
            // Perform Needed transformations
            var labeledTrees = CreateLabeledTrees(config.Paths.InData, config.Paths.OutModel);

            // Calculate and save PCFG to file
            var pCFGComposer = new LabeledTreePCFGComposer(labeledTrees.ToList(), config);
            pCFGComposer.CalculateProbabilities();


            ToCSWriter writer = new ToCSWriter(config.Paths.OutIdioms);
            
            var sampler = new TBSampler(writer, config);
            
            sampler.Initialize(pCFGComposer, labeledTrees);
            sampler.Train();

            if (saveModel)
            {
                foreach (var tree in sampler.Trees)
                {
                    tree.Serialize();
                }
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
            catch (Exception)
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

        static LabeledTree[] CreateLabeledTrees(string sourceDirectory, string outputDirectory)
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
