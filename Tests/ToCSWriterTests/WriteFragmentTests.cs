using NUnit.Framework;
using RoseLibML;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS;
using RoseLibML.CS.CSTrees;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RoseLibMLTests.ToCSWriterTests
{
    class WriteFragmentTests
    {
        [Test]
        public void FindRootMatchAndWriteFragmentTest()
        {
            var outPath = Path.Combine(Directory.GetCurrentDirectory(), "TestFiles\\TestOutputFile.txt");
            var toCSWriter = new ToCSWriter(outPath);

            var path = Path.Combine(Directory.GetCurrentDirectory(), "TestFiles\\TestFile.cs");
            var fileInfo = new FileInfo(path);
            var tree = CSTreeCreator.CreateTree(fileInfo, null);
            toCSWriter.Initialize(null, new LabeledTree[] { tree });

            var method1 = FindMethod1(tree.Root);
            Assert.IsNotNull(method1);

            var containingElement = method1.Parent.Parent;
            var leaves = toCSWriter.RetrieveFragmentLeaves(containingElement as CSNode);

            Assert.True(leaves.Count > 0);

            var anyLeavesToWrite = leaves.Any(l => l.CouldBeWritten);
            var anyLeavesWithAMatch = leaves.Any(l => l.IsExistingRoslynNode && l.UseRoslynMatchToWrite);
            Assert.True(anyLeavesToWrite);
            Assert.True(anyLeavesWithAMatch);

            toCSWriter.FindRootMatchAndWriteFragment(containingElement as CSNode, leaves);
            // Take a look at TestFiles\TestOutputFile.txt to see the result.

        }

        private LabeledNode FindMethod1(LabeledNode node)
        {
            foreach (var child in node.Children)
            {
                if (child.STInfo == "Method1")
                {
                    return child;
                }
                else
                {
                    var descendant = FindMethod1(child);
                    if (descendant != null)
                    {
                        return descendant;
                    }
                }
            }

            return null;
        }
    }
}
