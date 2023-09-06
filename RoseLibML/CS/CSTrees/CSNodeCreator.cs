using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.CS.CSTrees
{
    public class CSNodeCreator: NodeCreator
    {
        private FixedNodeKinds? fixedNodeKinds = null;

        public CSNodeCreator(FixedNodeKinds? fixedNodeKinds = null) 
        {
            if(fixedNodeKinds!= null)
            {
                this.fixedNodeKinds = fixedNodeKinds;
            }
        }
        public override LabeledNode CreateTempNode(string STInfo)   
        {
            var node = new CSNode()
            {
                UseRoslynMatchToWrite = true, // This is on purpose, so it wouldn't get written
                STInfo = STInfo,
            };

            SetIsFixed(node);

            return node;
        }
        public CSNode CreateNode(SyntaxNode parent)
        {
            var children = parent.ChildNodesAndTokens();
            var node = new CSNode();

            node.STInfo = ((ushort)parent.Kind()).ToString();
            node.UseRoslynMatchToWrite = false;
            SetIsFixed(node);

            node.RoslynSpanStart = parent.Span.Start;
            node.RoslynSpanEnd = parent.Span.End;

            if(children.Count == 0)
            {
                node.IsTreeLeaf= true;
            }
            foreach (var child in children)
            {
                if (child.IsNode)
                {
                    node.AddChild(CreateNode(child.AsNode()));
                }
                else if (child.IsToken)
                {
                    var tokenNode = new CSNode();

                    // If it is not an identifier token, you shoud try to find the
                    // match in the rosyn tree, and use it as a basis for writing.
                    if (child.AsToken().Kind() != SyntaxKind.IdentifierToken)
                    {
                        tokenNode.STInfo = child.AsToken().ValueText;
                        tokenNode.RoslynSpanStart = child.Span.Start;
                        tokenNode.RoslynSpanEnd = child.Span.End;
                        tokenNode.UseRoslynMatchToWrite = true;

                        tokenNode.CanHaveType = false;
                        tokenNode.IsTreeLeaf = true;
                    }
                    // If it is an indentifier token, it's value is being stored in STInfo.
                    // So, just use this value when writing, don't bother finding a match.
                    else
                    {
                        if(parent.Kind() == SyntaxKind.IdentifierName)
                        {
                            tokenNode.STInfo = child.AsToken().ValueText;
                            tokenNode.UseRoslynMatchToWrite = false;
                            tokenNode.CanHaveType = false;
                            tokenNode.IsTreeLeaf = true;
                        }
                        else
                        {
                            tokenNode.STInfo = child.AsToken()
                                            .Kind()
                                            .ToString();
                            tokenNode.UseRoslynMatchToWrite = false;
                            tokenNode.CanHaveType = true;

                            var leaf = new CSNode();
                            leaf.STInfo = child.AsToken().ValueText;
                            leaf.UseRoslynMatchToWrite = false;
                            leaf.CanHaveType = false;
                            leaf.IsTreeLeaf = true;

                            tokenNode.AddChild(leaf);
                        }
                    }

                    node.AddChild(tokenNode);
                }
            }

            return node;
        }

        private void SetIsFixed(CSNode node)
        {
            if (fixedNodeKinds != null
                            && fixedNodeKinds.FixedCutNodeKinds != null
                            && fixedNodeKinds.FixedCutNodeKinds.Contains(node.STInfo)
                            )
            {
                node.IsFixed = true;
                node.IsFragmentRoot = true;
            }

            if (fixedNodeKinds != null
                && fixedNodeKinds.FixedNonCutNodeKinds != null
                && fixedNodeKinds.FixedNonCutNodeKinds.Contains(node.STInfo)
                )
            {
                node.IsFixed = true;
                node.IsFragmentRoot = false;
            }
        }
    }
}
