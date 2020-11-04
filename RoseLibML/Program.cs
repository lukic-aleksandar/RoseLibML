using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using RoseLibML;
using RoseLibML.LanguageServer;
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
        static async Task Main(string[] args)
        {
            var server = await LanguageServer.From(options =>
                options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .WithLoggerFactory(new LoggerFactory())
                    .AddDefaultLoggingProvider()
                    .WithHandler<CommandHandler>()
                 );

            await server.WaitForExit;
        }

        /*static void Main(string[] args)
        {
            //ApplicationCommands.cs

            //SyntaxTree atree = null;
            //using (StreamReader sr = new StreamReader(@"C:\Users\nenad\Desktop\Nnd\doktorske\training1\ApplicationCommands.cs"))
            //{
            //    var source = sr.ReadToEnd();
            //    atree = CSharpSyntaxTree.ParseText(source);
            //}
            //var root = atree.GetRoot();
            // Create Labeled Trees
            // Perform Needed transformations
            var labeledTrees = CreateLabeledTrees(@"C:\Users\nenad\Desktop\Nnd\doktorske\training1", @"C:\Users\nenad\Desktop\Nnd\doktorske\out1000");

            // Calculate and save PCFG to file
            var pCFGComposer = new LabeledTreePCFGComposer(labeledTrees.ToList());
            pCFGComposer.CalculateProbabilities();
            // TODO: Save from Composer to file
            pCFGComposer.Serialize("LabeledTreeNodePCFGComposer.bin");
          

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

        static LabeledTree[] CreateLabeledTrees(string sourceDirectory, string outputDirectory)
        {
            var directoryInfo = new DirectoryInfo(sourceDirectory);
            var files = directoryInfo.GetFiles();

            LabeledTree[] labeledTrees = new LabeledTree[files.Length];
            
            Parallel.For(0, files.Length, (index) =>
            {

                var labeledTree = LabeledTree.CreateLabeledTree(files[index], outputDirectory);
                LabeledTreeTransformations.Binarize(labeledTree.Root);
                labeledTrees[index] = labeledTree;

                if (index % 100 == 0)
                {
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine($"File: {index}");
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                }
            });
           
            return labeledTrees;
        }*/
    }
}
