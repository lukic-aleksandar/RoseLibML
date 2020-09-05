using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoseLibML;
using System;
using System.Collections.Generic;
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
            //ApplicationCommands.cs

            // Create Labeled Trees
            // Perform Needed transformations
            var labeledTrees = CreateLabeledTrees(@"C:\Users\nenad\Desktop\Nnd\doktorske\training1");

            // Calculate and save PCFG to file
            var pCFGComposer = new LabeledTreePCFGComposer(labeledTrees.ToList());
            pCFGComposer.CalculateProbabilities();
            // TODO: Print from Composer to file

            // TODO: Use existing PCFG, if it exists, and trees 
            var sampler = new GibbsSampler();
            //sampler.Initialize(@"C:\Users\93luk\Desktop\RoseLibMLTraining\training1000", @"C:\Users\93luk\Desktop\RoseLibMLTraining\output1000");
            sampler.Initialize(pCFGComposer, labeledTrees);
            sampler.Train(3);

            foreach (var tree in sampler.Trees)
            {
                tree.Serialize();
            }

            Console.ReadKey();
        }

        static LabeledTree[] CreateLabeledTrees(string sourceDirectory)
        {
            var directoryInfo = new DirectoryInfo(sourceDirectory);
            var files = directoryInfo.GetFiles();

            LabeledTree[] labeledTrees = new LabeledTree[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                var labeledTree = LabeledTree.CreateLabeledTree(files[i], @"C:\Users\nenad\Desktop\Nnd\doktorske\out1000");
                LabeledTreeTransformations.Binarize(labeledTree.Root);
                labeledTrees[i] = labeledTree;
            }

            return labeledTrees;
        }
    }
}
