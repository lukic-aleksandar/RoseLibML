using MathNet.Numerics.Distributions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoseLibML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLib
{
    public class RoslynPCFGComposer: IPCFGComposer<SyntaxNode>
    {
        public Dictionary<string, Dictionary<string, PCFGNode>> Rules { get; set; }
        public string Path { get; set; }
        public double P { get; set; } = 0.0001;

        public delegate List<string> NodeProcessor(SyntaxNodeOrToken nodeOrToken);

        public RoslynPCFGComposer(string fileOrDirPath)
        {
            Rules = new Dictionary<string, Dictionary<string, PCFGNode>>();
            Path = fileOrDirPath;
        }
        
        public void CalculateProbabilities(List<SyntaxNode> treeRoots)
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

        public void PrintRules(StreamWriter stream)
        {
            foreach (var lhs in Rules.Keys)
            {
                foreach(var rhs in Rules[lhs].Keys)
                {
                    var output = $"{lhs} --> {rhs} {Rules[lhs][rhs].Probability}";
                    stream.WriteLine(output);
                }
            }

            stream.Flush();
            stream.Close();
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

            return 0.0000001; //Returns 1 because of the multiplication, maybe some small probability 0.00001
        }

        public void Count()
        {
            if (Directory.Exists(Path))
            {
                var directoryInfo = new DirectoryInfo(Path);
                var startTime = DateTime.UtcNow;
                var cnt = 0;
                foreach (var file in directoryInfo.GetFiles())
                {
                    ProcessFile(file.FullName);
                    cnt++;

                    if (cnt % 100 == 0)
                    {
                        Console.WriteLine(cnt);
                    }
                }

                var endTime = DateTime.UtcNow;

                var timePassed = endTime - startTime;
            }
            else
            {
                ProcessFile(Path);
            }
        }

        internal double CalculateFragmentProbability(string part2Fragment)
        {
            throw new NotImplementedException();
        }

        private void ProcessFile(string path)
        {
            using (var sr = new StreamReader(path))
            {
                var source = sr.ReadToEnd();
                var tree = CSharpSyntaxTree.ParseText(source);

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
                    //results.AddRange(processToken.Invoke(child));
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
                var pcfgNode = new PCFGNode(rhs);
                Rules[kind][rhs] = pcfgNode;
            }

            Rules[kind][rhs].Increment();
        }
    }
}
