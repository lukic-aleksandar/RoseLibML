using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML
{
    public class ToCSharpTranslator
    {
        public BookKeeper BookKeeper { get; set; }
        public LabeledTree[] Trees { get; set; }
        public ToCSharpTranslator(BookKeeper bookKeeper, LabeledTree[] trees)
        {
            BookKeeper = bookKeeper;
            Trees = trees;
        }

        public void WriteSingleFragment(string fragmentInTreebankNotation)
        {
            var rootNode = RetrieveFragmentRootNode(fragmentInTreebankNotation);
            var leaves = RetrieveFragmentLeaves(rootNode);

            var anyLeavesToWrite = leaves.Any(l => l.CouldBeWritten);
            var anyLeavesWithAMatch = leaves.Any(l => l.IsExistingRoslynNode && l.UseRoslynMatchToWrite);
            if (anyLeavesToWrite && anyLeavesWithAMatch)
            {
                FindRootMatchAndWriteFragment(rootNode, leaves.Where(l => l.CouldBeWritten));
            }
        }

        private LabeledNode RetrieveFragmentRootNode(string fragmentInTreebankNotation)
        {
            var typeKVPart2 = BookKeeper.TypeNodes.Where(kvp => kvp.Key.Part2Fragment == fragmentInTreebankNotation
                                                            && kvp.Value.Count > 0).FirstOrDefault();
            if (typeKVPart2.Value != null)
            {
                return typeKVPart2.Value.First();
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
                        var ancestor = nodeFull.Parent;
                        while (ancestor != null && !ancestor.IsFragmentRoot)
                        {
                            ancestor = ancestor.Parent;
                        }
                        return ancestor;
                    }
                    else
                    {
                        return nodeFull;
                    }
                }
            }

            return null;
        }

        private List<LabeledNode> RetrieveFragmentLeaves(LabeledNode rootNode)
        {
            Queue<LabeledNode> nodeQueue = new Queue<LabeledNode>(rootNode.Children);

            var leaves = new List<LabeledNode>();
            while (nodeQueue.Count > 0)
            {
                var node = nodeQueue.Dequeue();
                if(node.IsFragmentRoot || node.Children.Count == 0)
                {
                    leaves.Add(node);
                }
                else if (!node.IsFragmentRoot)
                {
                    using (var en = node.Children.GetEnumerator())
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

        private void FindRootMatchAndWriteFragment(LabeledNode fragmentRootNode, IEnumerable<LabeledNode> fragmentLeaves)
        {
            var withCorrespondingNode = RetrieveOneWithCoressponding(fragmentRootNode);
            var roslynTree = RetrieveRoslynTree(withCorrespondingNode);
            var root = roslynTree.GetRoot();
            var roslynFragmentRootNode = FindCorrespondingRoslynNodeOrToken(root.ChildNodesAndTokens(), withCorrespondingNode);

            if (roslynFragmentRootNode != null)
            {
                WriteLeaves(fragmentLeaves, roslynFragmentRootNode);
            }
            else
            {
                throw new Exception("Could not find node corresponding to fragment root!");
            }
        }

        private void WriteLeaves(IEnumerable<LabeledNode> fragmentLeaves, SyntaxNodeOrToken roslynFragmentRoot)
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("--------------- Fragment Start ---------------");
            Console.WriteLine();

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

                Console.Write(strWriter.ToString());
            }
        }

        private SyntaxTree RetrieveRoslynTree(LabeledNode fragmentRootNode)
        {
            var treeRoot = fragmentRootNode;
            while (treeRoot.Parent != null)
            {
                treeRoot = treeRoot.Parent;
            }

            var fragmentTree = Trees.Where(tree => tree.Root == treeRoot).FirstOrDefault();
            using (StreamReader sr = new StreamReader(fragmentTree.SourceFilePath))
            {
                var source = sr.ReadToEnd();
                return CSharpSyntaxTree.ParseText(source);
            }
        }

        // TODO: Consider transforming to binary search
        private SyntaxNodeOrToken FindCorrespondingRoslynNodeOrToken(IEnumerable<SyntaxNodeOrToken> syntaxNodesAndTokens, LabeledNode forNode)
        {
            foreach (var nodeOrToken in syntaxNodesAndTokens)
            {
                if (nodeOrToken.Span.Start == forNode.RoslynSpanStart && nodeOrToken.Span.End == forNode.RoslynSpanEnd)
                {
                    return nodeOrToken;
                }
                else if (nodeOrToken.Span.Start <= forNode.RoslynSpanStart && forNode.RoslynSpanEnd <= nodeOrToken.Span.End)
                {
                    return FindCorrespondingRoslynNodeOrToken(nodeOrToken.ChildNodesAndTokens(), forNode);
                }
            }

            return null;
        }


        private LabeledNode RetrieveOneWithCoressponding(LabeledNode node)
        {
            while (!node.IsExistingRoslynNode)
            {
                node = node.Parent;
            }

            return node;
        }
        

        
    }
}
