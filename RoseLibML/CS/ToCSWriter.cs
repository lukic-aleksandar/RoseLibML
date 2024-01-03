using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoseLibML.Core;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS.CSTrees;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.CS
{
    public class ToCSWriter : Writer
    {
        public BookKeeper BookKeeper { get; set; }
        public LabeledTree[] Trees { get; set; }
        public string OutputFile { get; set; }
        private StreamWriter StreamWriter { get; set; }
        private int CurrentIteration { get; set; } = -1;

        private int lengthThreshold;

        public Dictionary<int, List<string>> FragmentsPerIteration = new Dictionary<int, List<string>>();
        private bool areAnnouncementsNeeded = true;

        public ToCSWriter(string outputFile, int lengthThreshold)
        {
            OutputFile = outputFile;
            this.lengthThreshold = lengthThreshold;

            FragmentsPerIteration = new Dictionary<int, List<string>>();
        }

        public ToCSWriter()
        {
        }

        public void InitializeForSampler(BookKeeper bookKeeper, LabeledTree[] trees)
        {
            BookKeeper = bookKeeper;
            Trees = trees;

            new FileInfo(OutputFile).Directory.Create();
            StreamWriter = new StreamWriter(OutputFile, true);
        }

        public void InitializeForSingleIdiomUsage(LabeledTree[] trees)
        {
            Trees = trees;
            StreamWriter = new StreamWriter(new MemoryStream());
            areAnnouncementsNeeded = false;
        }

        public void SetIteration(int iteration)
        {
            if (iteration != CurrentIteration)
            {
                CurrentIteration = iteration;
                AnnounceNewIteration();
            }
        }

        public void WriteSingleFragment(string fragmentInTreebankNotation)
        {
            var rootNode = RetrieveFragmentRootNode(fragmentInTreebankNotation);
            var leaves = RetrieveFragmentLeaves(rootNode);

            var anyLeavesToWrite = leaves.Any(l => l.CouldBeWritten);
            var anyLeavesWithAMatch = leaves.Any(l => l.IsExistingRoslynNode && l.UseRoslynMatchToWrite);
            if (anyLeavesToWrite && anyLeavesWithAMatch)
            {
                FindRootMatchAndWriteFragment(rootNode, leaves);
                //FindRootMatchAndWriteFragment(rootNode, leaves.Where(l => l.CouldBeWritten));
            }
        }

        public string SingleFragmentToString(CSNode rootNode)
        {
            var leaves = RetrieveFragmentLeaves(rootNode);

            var anyLeavesToWrite = leaves.Any(l => l.CouldBeWritten);
            var anyLeavesWithAMatch = leaves.Any(l => l.IsExistingRoslynNode && l.UseRoslynMatchToWrite);
            if (anyLeavesToWrite && anyLeavesWithAMatch)
            {
                FindRootMatchAndWriteFragment(rootNode, leaves);
            }

            return Encoding.ASCII.GetString((StreamWriter.BaseStream as MemoryStream)!.ToArray());
        }

        private CSNode RetrieveFragmentRootNode(string fragmentInTreebankNotation)
        {
            // Looking for a node that will serve as a root node for writing
            // Using types to find such a node


            // Finding it using a type that has Part2 equal to passed fragment seems easiest
            // Just take a node with such a type as a root
            // There could be a side-effect here! What if such a root is not a fragment root? Explore the possibilities.
            var typeKVPart2 = BookKeeper.TypeNodes.Where(kvp => kvp.Key.Part2Fragment == fragmentInTreebankNotation
                                                            && kvp.Value.Count > 0).FirstOrDefault();
            if (typeKVPart2.Value != null)
            {
                return typeKVPart2.Value.First() as CSNode;
            }


            // Finding it using a type that has a same full fragment.
            // If there is such a type, next thing could pose a problem! For writing mechanism to know when to 
            // stop writing, it uses "IsFragmentRoot". So, a node which has such type, should not be a fragment root.
            // When u find such type and node, find the ancestor that is eather a fragment root, or a full tree root.
            var typeKVFulls = BookKeeper.TypeNodes.Where(kvp => kvp.Key.FullFragment == fragmentInTreebankNotation
                                                            && kvp.Value.Count > 0);

            foreach (var typeKV in typeKVFulls)
            {
                var nonCuttingNode = typeKV.Value.Where(node => node.IsFragmentRoot == false).FirstOrDefault();
                if (nonCuttingNode != null)
                {
                    var ancestor = nonCuttingNode.Parent; // Tree root node, which doesn't have a parent, can't have a type, so I'm not taking that case into consideration
                    while (ancestor.IsFragmentRoot != false && ancestor.Parent != null)
                    {
                        ancestor = ancestor.Parent;
                    }

                    return ancestor as CSNode;
                }
            }


            // Finding it using a type that has a same Part 1 fragment.
            // Similar to the full fragment case, but there is also a catch!
            // A node which has such a part1 type, must also be a fragment root! Because writing mechanism must know when to stop.
            var typeKVPart1s = BookKeeper.TypeNodes.Where(kvp => kvp.Key.Part1Fragment == fragmentInTreebankNotation
                                                            && kvp.Value.Count > 0);

            foreach (var typeKVPart1 in typeKVPart1s)
            {
                var cuttingNode = typeKVPart1.Value.Where(node => node.IsFragmentRoot == true).FirstOrDefault();
                if (cuttingNode != null)
                {
                    if (cuttingNode.Parent != null)
                    {
                        var ancestor = cuttingNode.Parent;
                        while (ancestor.IsFragmentRoot != false && ancestor.Parent != null)
                        {
                            ancestor = ancestor.Parent;
                        }

                        return ancestor as CSNode;
                    }
                }
            }


            return null;
        }

        public List<CSNode> RetrieveFragmentLeaves(CSNode rootNode)
        {
            var rootChildren = new List<CSNode>(rootNode.Children.Cast<CSNode>());
            rootChildren.Reverse();
            Stack<CSNode> nodeStack = new Stack<CSNode>(rootChildren);


            var leaves = new List<CSNode>();
            while (nodeStack.Count > 0)
            {
                var node = nodeStack.Pop();
                if (node.IsFragmentRoot || node.Children.Count == 0)
                {
                    leaves.Add(node);
                }
                else if (!node.IsFragmentRoot)
                {
                    var children = new List<CSNode>(node.Children.Cast<CSNode>());
                    children.Reverse();
                    using (var en = children.Cast<CSNode>().GetEnumerator())
                    {
                        while (en.MoveNext())
                        {
                            nodeStack.Push(en.Current);
                        }
                    }
                }
            }

            return leaves;
        }


        public void FindRootMatchAndWriteFragment(CSNode fragmentRootNode, IEnumerable<CSNode> fragmentLeaves)
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
            //var writableLeaves = fragmentLeaves.Where(l => l.CouldBeWritten);
            var writableLeaves = fragmentLeaves;

            using (StringWriter strWriter = new StringWriter())
            {
                var unknownMetaNamedMetaCount = 0;
                foreach (var leaf in writableLeaves)
                {
                    if (leaf.IsExistingRoslynNode && leaf.UseRoslynMatchToWrite)
                    {
                        if(leaf.IsTreeLeaf)
                        {
                            strWriter.Write($" {leaf.STInfo} ");
                        }
                        else
                        {
                            var roslynNode = FindCorrespondingRoslynNodeOrToken(roslynFragmentRoot.ChildNodesAndTokens(), leaf);
                            if (roslynNode != null)
                            {
                                strWriter.Write(roslynNode.ToFullString());
                            }
                        }
                    }
                    else if (leaf.IsExistingRoslynNode)
                    {
                        if (leaf.IsTreeLeaf)
                        {
                            strWriter.Write($" {leaf.STInfo} ");
                        }
                        else
                        {
                            var uShortSyntaxKind = ushort.Parse(leaf.STInfo);
                            strWriter.Write($" ${(SyntaxKind)uShortSyntaxKind} ");
                        }
                    }
                    else if (leaf.UseRoslynMatchToWrite)
                    {
                        if (leaf.STInfo.StartsWith("B_"))
                        {
                            var uShortSyntaxKindOfMeta = ushort.Parse(leaf.STInfo.Substring("B_".Length));
                            strWriter.Write($" ${(SyntaxKind)uShortSyntaxKindOfMeta} ");
                        }
                        else
                        {
                            strWriter.Write($" $meta{++unknownMetaNamedMetaCount}");
                        }
                    }
                    else
                    {
                        if (leaf.ToString() == "IdentifierToken")
                        {
                            strWriter.Write($" ${leaf.ToString()} ");

                        }
                        else
                        {
                            strWriter.Write($" {leaf.ToString()} ");
                        }
                    }

                }

                var fragment = strWriter.ToString();
                if(fragment.Length >= lengthThreshold)
                {
                    AnnounceAndWriteNewFragment(roslynFragmentRoot, fragment);

                    // in order to show in the vs code extension
                    AddFragmentToDictionary(CurrentIteration, strWriter.ToString());
                }
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
                    if (nodeOrToken.ChildNodesAndTokens().Count != 0)
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
                StreamWriter.Flush();


                //Console.Write(strWriter.ToString());
            }
        }

        private void AnnounceAndWriteNewFragment(SyntaxNodeOrToken roslynFragmentRoot, string fragment)
        {
            using (StringWriter strWriter = new StringWriter())
            {
                if(areAnnouncementsNeeded)
                {
                    strWriter.WriteLine();
                    strWriter.WriteLine();
                    strWriter.WriteLine($"~~~ Fragment with root {roslynFragmentRoot.Kind()} in iteration {CurrentIteration} ~~~");
                    strWriter.WriteLine();

                    StreamWriter.Write(strWriter.ToString());
                }

                var json = $"{{ \n\t\"root\": \"{roslynFragmentRoot.Kind()}\", \n\t\"fragment\": \"\n{fragment}\n\" }}";
                StreamWriter.Write(json);
                StreamWriter.Flush();

                //Console.Write(strWriter.ToString());
                //Console.Write(fragment);
            }
        }

        private void AddFragmentToDictionary(int iteration, string fragment)
        {
            if (!FragmentsPerIteration.ContainsKey(iteration))
            {
                FragmentsPerIteration.Add(iteration, new List<string>());
            }

            FragmentsPerIteration[iteration].Add(fragment);
        }

        public void Close()
        {
            StreamWriter.Close();
        }
    }
}
