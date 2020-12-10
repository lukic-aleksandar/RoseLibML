using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoseLibML.Core.LabeledTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.CS.CSTrees
{
    public class CSNodeCreator: NodeCreator
    {
        public override LabeledNode CreateNode()
        {
            var node = new CSNode()
            {
                UseRoslynMatchToWrite = true // This is on purpose, so it wouldn't get written
            };
            return node;
        }
        public static CSNode CreateNode(SyntaxNode parent)
        {
            var children = parent.ChildNodesAndTokens();
            var treeNode = new CSNode();

            treeNode.STInfo = ((ushort)parent.Kind()).ToString();
            treeNode.UseRoslynMatchToWrite = false;
            treeNode.RoslynSpanStart = parent.Span.Start;
            treeNode.RoslynSpanEnd = parent.Span.End;

            foreach (var child in children)
            {
                if (child.IsNode)
                {
                    treeNode.AddChild(CreateNode(child.AsNode()));
                }
                else if (child.IsToken)
                {
                    var tokenNode = new CSNode();

                    if (child.AsToken().Kind() != SyntaxKind.IdentifierToken)
                    {
                        tokenNode.STInfo = ((ushort)child.AsToken()
                                            .Kind())
                                            .ToString();
                        tokenNode.RoslynSpanStart = child.Span.Start;
                        tokenNode.RoslynSpanEnd = child.Span.End;
                        tokenNode.UseRoslynMatchToWrite = true;

                        tokenNode.CanHaveType = false;
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

                        tokenNode.AddChild(leaf);
                    }

                    treeNode.AddChild(tokenNode);
                }
            }

            return treeNode;
        }
    }
}
