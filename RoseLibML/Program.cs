using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoseLibML;
using RoseLibML.CS.CSTrees;
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
            //ApplicationCommands.cs for debug purposess

            // Create Labeled Trees
            // Perform Needed transformations
            var labeledTrees = CreateLabeledTrees(@"C:\Users\nenad\Desktop\Nnd\doktorske\training1", @"C:\Users\nenad\Desktop\Nnd\doktorske\out1000");

            // Calculate and save PCFG to file
            var pCFGComposer = new LabeledTreePCFGComposer(labeledTrees.ToList());
            pCFGComposer.CalculateProbabilities();
            // TODO: Save from Composer to file
            //pCFGComposer.Serialize("LabeledTreeNodePCFGComposer.bin");


            // TODO: Use existing PCFG, if it exists, and trees 
            ToCSWriter writer = new ToCSWriter(@"C:\Users\nenad\Desktop\Nnd\doktorske\out1000\idioms\idioms1.txt");
            var sampler = new GibbsSampler(writer);
            //sampler.Initialize(@"C:\Users\93luk\Desktop\RoseLibMLTraining\training1000", @"C:\Users\93luk\Desktop\RoseLibMLTraining\output1000");
            sampler.Initialize(pCFGComposer, labeledTrees);
            sampler.Train(5, 3, 5);

            foreach (var tree in sampler.Trees)
            {
                tree.Serialize();
            }

            Console.ReadKey();
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
