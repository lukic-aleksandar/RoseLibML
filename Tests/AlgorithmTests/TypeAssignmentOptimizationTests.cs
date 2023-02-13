using NUnit.Framework;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS.CSTrees;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RoseLibMLTests.AlgorithmTests.SupportingClasses;

namespace RoseLibMLTests.AlgorithmTests
{
    class TypeAssignmentOptimizationTests
    {
        /*
        [Test, Ignore("Needs to be run on purpose. Can take a lot of time to run.")]
        public void TestThatGivenAndTrueTypeAreEqual()
        {
            var labeledTrees = CreateLabeledTrees(@"C:\Users\nenad\Desktop\Nnd\doktorske\sets\testgibbs2", @"C:\Users\nenad\Desktop\Nnd\doktorske\sets\out1000");

            var pCFGComposer = new LabeledTreePCFGComposer(labeledTrees.ToList());
            pCFGComposer.CalculateProbabilities();

            ToCSWriter writer = new ToCSWriter(@"C:\Users\nenad\Desktop\Nnd\doktorske\out1000\idioms\idioms2.txt");
            var sampler = new TBSampler(writer);
            sampler.BookKeeper = new ExtendedBookKeeper();
            sampler.Initialize(pCFGComposer, labeledTrees);
            
            try
            {
                sampler.Train(10, 10, 3);
            }
            catch (ArgumentException e)
            {
                Assert.Fail(e.Message);
            }

            Assert.Pass();
        }
        */
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
