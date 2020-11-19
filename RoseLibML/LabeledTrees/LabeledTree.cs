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
    public class LabeledTree
    {
        public LabeledTreeNode Root { get; set; }
        public string SourceFilePath { get; set; }
        public string FilePath { get; set; }

        public void Serialize()
        {
            Root.Serialize(FilePath);
        }

        public static LabeledTree CreateLabeledTree(FileInfo sourceInfo, string outDirectory)
        {
            using (StreamReader sr = new StreamReader(sourceInfo.FullName))
            {
                var source = sr.ReadToEnd();
                var tree = CSharpSyntaxTree.ParseText(source);

                var labeledTree = new LabeledTree();
                
                labeledTree.SourceFilePath = sourceInfo.FullName;
                if(outDirectory != null)
                {
                    labeledTree.FilePath = $"{outDirectory}/{sourceInfo.Name}.bin";
                }
                
                labeledTree.Root = CreateLabeledNode(tree.GetRoot());
                labeledTree.Root.IsFragmentRoot = true;
                labeledTree.Root.CanHaveType = false;

                return labeledTree;
            }
        }

        private static LabeledTreeNode CreateLabeledNode(SyntaxNode parent)
        {
            var children = parent.ChildNodesAndTokens();
            var treeNode = new LabeledTreeNode();

            treeNode.LTType = ((ushort)parent.Kind()).ToString();
            treeNode.UseRoslynMatchToWrite = false;
            treeNode.RoslynSpanStart = parent.Span.Start;
            treeNode.RoslynSpanEnd = parent.Span.End;

            foreach (var child in children)
            {
                if (child.IsNode)
                {
                    treeNode.AddChild(CreateLabeledNode(child.AsNode()));
                }
                else if (child.IsToken)
                {
                    var tokenNode = new LabeledTreeNode();

                    if(child.AsToken().Kind() != SyntaxKind.IdentifierToken)
                    {
                        tokenNode.LTType = ((ushort)child.AsToken()
                                            .Kind())
                                            .ToString();
                        tokenNode.RoslynSpanStart = child.Span.Start;
                        tokenNode.RoslynSpanEnd = child.Span.End;
                        tokenNode.UseRoslynMatchToWrite = true;

                        tokenNode.CanHaveType = false;
                    }
                    else
                    {
                        tokenNode.LTType = child.AsToken()
                                            .Kind()
                                            .ToString();
                        tokenNode.UseRoslynMatchToWrite = false;
                        tokenNode.CanHaveType = true;

                        var leaf = new LabeledTreeNode();
                        leaf.LTType = child.AsToken().ValueText;
                        leaf.UseRoslynMatchToWrite = false;
                        leaf.CanHaveType = false;
                        
                        tokenNode.AddChild(leaf);
                    }

                    treeNode.AddChild(tokenNode);
                }
            }

            return treeNode;
        }
    }
}
