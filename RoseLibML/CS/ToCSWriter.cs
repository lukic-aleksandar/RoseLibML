using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoseLibML.Core;
using RoseLibML.CS.CSTrees;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML
{
    public class ToCSWriter : Writer
    {
        public BookKeeper BookKeeper { get; set; }
        public LabeledTree[] Trees { get; set; }
        public string OutputFile { get; set; }
        private StreamWriter StreamWriter{ get; set; }
        private int CurrentIteration { get; set; } = -1;

        public ToCSWriter(string outputFile)
        {
            OutputFile = outputFile;
        }

        public void Initialize(BookKeeper bookKeeper, LabeledTree[] trees)
        {
            BookKeeper = bookKeeper;
            Trees = trees;

            (new FileInfo(OutputFile)).Directory.Create();
            StreamWriter = new StreamWriter(OutputFile, true);
        }

        public void WriteSingleFragment(string fragmentInTreebankNotation, int iteration)
        {
            if(iteration != CurrentIteration)
            {
                CurrentIteration = iteration;
                AnnounceNewIteration();
            }

            var rootNode = RetrieveFragmentRootNode(fragmentInTreebankNotation);
            var leaves = RetrieveFragmentLeaves(rootNode);

            var anyLeavesToWrite = leaves.Any(l => l.CouldBeWritten);
            var anyLeavesWithAMatch = leaves.Any(l => l.IsExistingRoslynNode && l.UseRoslynMatchToWrite);
            if (anyLeavesToWrite && anyLeavesWithAMatch)
            {
                FindRootMatchAndWriteFragment(rootNode, leaves.Where(l => l.CouldBeWritten));
            }
        }

        private CSNode RetrieveFragmentRootNode(string fragmentInTreebankNotation)
        {
            var typeKVPart2 = BookKeeper.TypeNodes.Where(kvp => kvp.Key.Part2Fragment == fragmentInTreebankNotation
                                                            && kvp.Value.Count > 0).FirstOrDefault();
            if (typeKVPart2.Value != null)
            {
                return typeKVPart2.Value.First() as CSNode;
            }
            else
            {
                var typeKVFulls = BookKeeper.TypeNodes.Where(kvp => kvp.Key.FullFragment == fragmentInTreebankNotation
                                                            && kvp.Value.Count > 0);
                foreach (var typeKV in typeKVFulls)
                {
                    var nodeFull = typeKV.Value.Where(node => (node.Parent != null && node.IsFragmentRoot == false)
                                                    || (node.Parent == null && node.IsFragmentRoot == true)).FirstOrDefault();
                    if (nodeFull != null && nodeFull.IsFragmentRoot == false)
                    {
                        var ancestor = nodeFull.Parent as CSNode;
                        while (ancestor != null && (!ancestor.IsFragmentRoot && !ancestor.IsTreeRoot()))
                        {
                            ancestor = ancestor.Parent as CSNode;
                        }
                        return ancestor;
                    }
                    else
                    {
                        return nodeFull as CSNode;
                    }
                }
            }

            var typeKVPart1 = BookKeeper.TypeNodes.Where(kvp => kvp.Key.Part1Fragment == fragmentInTreebankNotation
                                                            && kvp.Value.Count > 0).FirstOrDefault();
            if (typeKVPart1.Value != null)
            {
                return typeKVPart1.Value.First() as CSNode;
            }

            return null;
        }

        private List<CSNode> RetrieveFragmentLeaves(CSNode rootNode)
        {
            Queue<CSNode> nodeQueue = new Queue<CSNode>(rootNode.Children.Cast<CSNode>());

            var leaves = new List<CSNode>();
            while (nodeQueue.Count > 0)
            {
                var node = nodeQueue.Dequeue();
                if(node.IsFragmentRoot || node.Children.Count == 0)
                {
                    leaves.Add(node);
                }
                else if (!node.IsFragmentRoot)
                {
                    using (var en = node.Children.Cast<CSNode>().GetEnumerator())
                    {
                        while (en.MoveNext())
                        {
                            nodeQueue.Enqueue(en.Current);
                        }
                    }
                }
            }

            return leaves;
        }

        private void FindRootMatchAndWriteFragment(CSNode fragmentRootNode, IEnumerable<CSNode> fragmentLeaves)
        {
            var withCorrespondingNode = RetrieveOneWithCoressponding(fragmentRootNode);
            var roslynTree = RetrieveRoslynTree(withCorrespondingNode);
            var root = roslynTree.GetRoot();
            var roslynFragmentRootNode = FindCorrespondingRoslynNodeOrToken(new List<SyntaxNodeOrToken>() { root }, withCorrespondingNode);

            if (roslynFragmentRootNode != null)
            {
                WriteLeaves(fragmentLeaves, roslynFragmentRootNode);
            }
            else
            {
                throw new Exception("Could not find node corresponding to fragment root!");
            }
        }

        private void WriteLeaves(IEnumerable<CSNode> fragmentLeaves, SyntaxNodeOrToken roslynFragmentRoot)
        {
            var writableLeaves = fragmentLeaves.Where(l => l.CouldBeWritten);
            using (StringWriter strWriter = new StringWriter())
            {
                foreach (var leaf in writableLeaves)
                {
                    if(leaf.IsExistingRoslynNode && leaf.UseRoslynMatchToWrite)
                    {
                        var roslynNode = FindCorrespondingRoslynNodeOrToken(roslynFragmentRoot.ChildNodesAndTokens(), leaf);
                        if (roslynNode != null)
                        {
                            strWriter.Write(roslynNode.ToFullString());
                        }
                    }
                    else if (leaf.IsExistingRoslynNode)
                    {
                        var uShortSyntaxKind = ushort.Parse(leaf.STInfo);
                        strWriter.Write((SyntaxKind)uShortSyntaxKind);
                    }
                    else {
                        strWriter.Write($" {leaf.ToString()}");
                    }
                    
                }

                AnnounceNewFragment(strWriter.ToString());
            }
        }

        private SyntaxTree RetrieveRoslynTree(CSNode fragmentRootNode)
        {
            var treeRoot = fragmentRootNode;
            while (treeRoot.Parent != null)
            {
                treeRoot = treeRoot.Parent as CSNode;
            }

            var fragmentTree = Trees.Where(tree => tree.Root == treeRoot).FirstOrDefault();
            using (StreamReader sr = new StreamReader(fragmentTree.SourceFilePath))
            {
                var source = sr.ReadToEnd();
                return CSharpSyntaxTree.ParseText(source);
            }
        }

        // TODO: Consider transforming to binary search
        private SyntaxNodeOrToken FindCorrespondingRoslynNodeOrToken(IEnumerable<SyntaxNodeOrToken> syntaxNodesAndTokens, CSNode forNode)
        {
            foreach (var nodeOrToken in syntaxNodesAndTokens)
            {
                if (nodeOrToken.Span.Start == forNode.RoslynSpanStart && nodeOrToken.Span.End == forNode.RoslynSpanEnd)
                {
                    return nodeOrToken;
                }
                else if (nodeOrToken.Span.Start <= forNode.RoslynSpanStart && forNode.RoslynSpanEnd <= nodeOrToken.Span.End)
                {
                    if(nodeOrToken.ChildNodesAndTokens().Count != 0)
                    {
                        return FindCorrespondingRoslynNodeOrToken(nodeOrToken.ChildNodesAndTokens(), forNode);
                    }                    
                }
            }

            return null;
        }


        private CSNode RetrieveOneWithCoressponding(CSNode node)
        {
            while (!node.IsExistingRoslynNode)
            {
                node = node.Parent as CSNode;
            }

            return node;
        }

        private void AnnounceNewIteration()
        {
            using (StringWriter strWriter = new StringWriter())
            {
                strWriter.WriteLine();
                strWriter.WriteLine();
                strWriter.WriteLine();
                strWriter.WriteLine();
                strWriter.WriteLine();

                strWriter.WriteLine($"----------> Iteration {CurrentIteration} <----------");

                strWriter.WriteLine();

                StreamWriter.Write(strWriter.ToString());
                StreamWriter.FlushAsync();


                Console.Write(strWriter.ToString());
            }
        }

        private void AnnounceNewFragment(string fragment)
        {
            using (StringWriter strWriter = new StringWriter())
            {
                strWriter.WriteLine();
                strWriter.WriteLine();
                strWriter.WriteLine($"~~~ Fragment in iteration {CurrentIteration} ~~~");
                strWriter.WriteLine();

                StreamWriter.Write(strWriter.ToString());
                StreamWriter.Write(fragment);
                StreamWriter.FlushAsync();

                Console.Write(strWriter.ToString());
                Console.Write(fragment);

            }
        }

        public void Close()
        {
            StreamWriter.Close();
        }
    }
}
