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
    // Dobro! Čini mi se da imaš situaciju u kojoj imaš 2,3 stanja. i to čuvaš u 2 različite promenljive
    // Koja je alternativa? Refaktorisati? Osloniti se na postojeće?
    // Šta bi meni trebalo? Trebalo bi mi da se $BinTempNode ispisuje. Ok je da se ne ispisuje uvek sam za sebe...
    // Međutim, u nekom trenutku sam hteo da uvedem i BinTempNodove koji odgovaraju konkretnom nadtipu! To je istina. 
    // Želja je bila da to uradim kako bih ipak malo uticao na verovatnoće, da ne budu tako problematične. Da li sam to uradio?
    // U Binarize metodi, koja se nalazi u LabeledTreeTransformations možeš videti da jesam! To care! :D 
    // Ali se tamo menja isključivo STInfo! Vidi da li možeš da izoluješ ove dve promenljive. I onda, da Uvedeš neku agregatnu.
    // Te dve promenljive su:
    // - UseRoslynMatchToWrite
    // - IsExistingRoslynNode
    // Druga promeljiva nije baš promenljiva. Zavisi od enumeracije, roslyn tipa
    // Problematični deo koda:
    // - 22 linija, gde za temp odmah pravim hak kako ga ne bi ispisao
    // 
    public class CSNodeCreator: NodeCreator
    {
        public override LabeledNode CreateTempNode()
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
            var node = new CSNode();

            node.STInfo = ((ushort)parent.Kind()).ToString();
            node.UseRoslynMatchToWrite = false;
            node.RoslynSpanStart = parent.Span.Start;
            node.RoslynSpanEnd = parent.Span.End;

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
                        tokenNode.STInfo = ((ushort)child.AsToken()
                                            .Kind())
                                            .ToString();
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

                    node.AddChild(tokenNode);
                }
            }

            return node;
        }
    }
}
