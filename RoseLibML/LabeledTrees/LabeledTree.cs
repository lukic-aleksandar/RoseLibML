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
    [Serializable]
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

            treeNode.ASTNodeType = parent.Kind().ToString();
            // treeNode.IsFragmentRoot = new Random().NextDouble() < CutProbability; // Must be done where needed


            foreach (var child in children)
            {
                if (child.IsNode)
                {
                    treeNode.AddChild(CreateLabeledNode(child.AsNode()));
                }
                else if (child.IsToken)
                {
                    var tokenNode = new LabeledTreeNode();
                    tokenNode.ASTNodeType = child.AsToken()
                                            .Kind()
                                            .ToString();
                    tokenNode.CanHaveType = false;
                    treeNode.AddChild(tokenNode);

                    if (child.AsToken().Kind() == SyntaxKind.IdentifierToken)
                    {
                        var leaf = new LabeledTreeNode();
                        leaf.ASTNodeType = child.AsToken().ValueText;
                        leaf.CanHaveType = false;
                        tokenNode.CanHaveType = true;
                        tokenNode.AddChild(leaf);
                    }
                }
            }

            return treeNode;
        }
    }
}
