using MathNet.Numerics.Distributions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLib
{
    public class PCFGComposer
    {
        public Dictionary<string, Dictionary<string, PCFGNode>> Rules { get; set; }
        public string FilePath { get; set; }
        public double P { get; set; } = 0.1;

        public delegate List<string> NodeProcessor(SyntaxNodeOrToken nodeOrToken);

        public PCFGComposer(string filePath)
        {
            Rules = new Dictionary<string, Dictionary<string, PCFGNode>>();
            FilePath = filePath;
        }

        public void CalculateProbabilities()
        {
            Count();

            foreach (var lhs in Rules.Keys)
            {
                var totalCount = 0.0;

                foreach (var rhs in Rules[lhs].Keys)
                {
                    totalCount += Rules[lhs][rhs].Count;
                }

                foreach (var rhs in Rules[lhs].Keys)
                {
                    Rules[lhs][rhs].Probability = Rules[lhs][rhs].Count / totalCount;
                }
            }
        }

        public double CalculateFragmentProbability(SyntaxNode root)
        {
            var fragmentSize = 0;
            var fragmentProbability = GetNonTerminalProbability(root, out fragmentSize);

            var dist = new Geometric(P);
            return dist.Probability(fragmentSize) * fragmentProbability;
        }

        private double GetNonTerminalProbability(SyntaxNode node, out int fragmentSize)
        {
            var kind = node.Kind().ToString();
            var children = node.ChildNodesAndTokens();
            fragmentSize = children.Count;

            var rhs = "";
            var probability = 1.0;

            foreach (var child in children)
            {
                if (child.IsNode)
                {
                    var childNode = child.AsNode();
                    var childFragmentSize = 0;
                    rhs += $"{childNode.Kind().ToString()} ";
                    probability *= GetNonTerminalProbability(childNode, out childFragmentSize);
                    fragmentSize += childFragmentSize;
                }
                else if (child.IsToken)
                {
                    var token = child.AsToken();
                    var tokenKind = token.Kind();
                    rhs += $"{tokenKind} ";
                    fragmentSize += 1;

                    if (tokenKind == SyntaxKind.IdentifierToken)
                    {
                        probability *= GetTerminalProbability(token);
                    }
                }
            }

            if(Rules.ContainsKey(kind) && Rules[kind].ContainsKey(rhs))
            {
                probability *= Rules[kind][rhs].Probability;
            }

            return probability;
        }
        private double GetTerminalProbability(SyntaxToken token)
        {
            var tokenKind = token.Kind().ToString();

            if (Rules.ContainsKey(tokenKind) && Rules[tokenKind].ContainsKey(token.ValueText))
            {
                return Rules[tokenKind][token.ValueText].Probability;
            }

            return 1; //Returns 1 because of the multiplication, maybe some small probability 0.00001
        }

        public void Count()
        {
            using(var sr = new StreamReader(FilePath))
            {
                var source = sr.ReadToEnd();
                var tree = CSharpSyntaxTree.ParseText(source);
                //Count(tree.GetRoot());

                TreeTraverse(tree.GetRoot(), ProcessNode, ProcessToken, PostProcessCount);
            }
        }

        private List<string> ProcessNode(SyntaxNodeOrToken element)
        {
            var node = element.AsNode();
            return new List<string> { node.Kind().ToString() };
        }

        private List<string> ProcessToken(SyntaxNodeOrToken element)
        {
            var token = element.AsToken();
            var tokenKind = token.Kind();
          
            if (tokenKind == SyntaxKind.IdentifierToken)
            {
                IncrementRuleCount(tokenKind.ToString(), token.ValueText, element);
            }
            
            return new List<string> { tokenKind.ToString() };
        }

        private void PostProcessCount(SyntaxNode parent, List<string> results)
        {
            var kind = parent.Kind().ToString();
            var children = parent.ChildNodesAndTokens();
            var rhs = "";

            foreach (var result in results)
            {
                rhs += result + " ";
            }

            IncrementRuleCount(kind, rhs.Trim(), children.ToArray());
        }

        private void TreeTraverse(SyntaxNode parent, NodeProcessor processNode, NodeProcessor processToken, Action<SyntaxNode, List<string>> postProcessor)
        {
            var children = parent.ChildNodesAndTokens();
            List<string> results = new List<string>();

            foreach (var child in children)
            {
                if (child.IsNode)
                {
                    results.AddRange(processNode.Invoke(child));
                    TreeTraverse(child.AsNode(), processNode, processToken, postProcessor);
                }
                else if (child.IsToken)
                {
                    results.AddRange(processToken.Invoke(child));
                }
            }

            postProcessor(parent, results);
        }

        private void Count(SyntaxNode parent)
        {
            var kind = parent.Kind().ToString();
            var children = parent.ChildNodesAndTokens();

            var rhs = "";

            foreach (var child in children)
            {
                if (child.IsNode)
                {
                    var node = child.AsNode();
                    rhs += $"{node.Kind().ToString()} ";
                    Count(node);

                }
                else if (child.IsToken)
                {
                    var token = child.AsToken();
                    var tokenKind = token.Kind();
                    rhs += $"{tokenKind} ";

                    if (tokenKind == SyntaxKind.IdentifierToken)
                    {
                        IncrementRuleCount(tokenKind.ToString(), token.ValueText, child);
                    }
                }
            }

            rhs = rhs.Trim();

            IncrementRuleCount(kind, rhs, children.ToArray());
        }

        private void IncrementRuleCount(string kind, string rhs, params SyntaxNodeOrToken[] nodes)
        {
            if (!Rules.ContainsKey(kind))
            {
                Rules.Add(kind, new Dictionary<string, PCFGNode>());
            }

            if (!Rules[kind].ContainsKey(rhs))
            {
                var pcfgNode = new PCFGNode(nodes, rhs);
                Rules[kind][rhs] = pcfgNode;
            }

            Rules[kind][rhs].Increment();
        }
    }
}
