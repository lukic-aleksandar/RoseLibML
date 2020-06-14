﻿using MathNet.Numerics.Distributions;
using RoseLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML
{
    [Serializable]
    public class LabeledTreePCFGComposer 
    { 
        public Dictionary<string, Dictionary<string, PCFGNode>> Rules { get; set; }
        List<LabeledTree> Trees { get; set; }

        public double P { get; set; } = 0.0001;

        public LabeledTreePCFGComposer(List<LabeledTree> trees)
        {
            Rules = new Dictionary<string, Dictionary<string, PCFGNode>>();
            Trees = trees;
        }

        public void CalculateProbabilities()
        {
            Count(Trees);

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

        public double CalculateFragmentProbability(LabeledTreeNode root)
        {
            var fragmentSize = 0;
            var fragmentProbability = GetNodeProbability(root, out fragmentSize);

            var dist = new Geometric(P);
            return dist.Probability(fragmentSize) * fragmentProbability;
        }

        public void PrintRules(StreamWriter stream)
        {
            foreach (var lhs in Rules.Keys)
            {
                foreach (var rhs in Rules[lhs].Keys)
                {
                    var output = $"{lhs} --> {rhs} {Rules[lhs][rhs].Probability}";
                    stream.WriteLine(output);
                }
            }

            stream.Flush();
            stream.Close();
        }

        private double GetNodeProbability(LabeledTreeNode node, out int fragmentSize)
        {
            var kind = node.ASTNodeType;
            var children = node.Children;
            fragmentSize = children.Count;

            var rhs = "";
            var probability = 1.0;

            if(children.Count == 0)
            {
                var tokenKind = node.Parent.ASTNodeType;
                
                if(tokenKind == "IdentifierToken")
                {
                    if (Rules.ContainsKey(tokenKind) && Rules[tokenKind].ContainsKey(node.ASTNodeType))
                    {
                        return Rules[tokenKind][node.ASTNodeType].Probability;
                    }

                    return 0.0000001;
                }

                return 1.0;
            }

            foreach (var child in children)
            {
                var childFragmentSize = 0;
                rhs += $"{child.ASTNodeType} ";
                probability *= GetNodeProbability(child, out childFragmentSize);
                fragmentSize += childFragmentSize;
            }

            if (Rules.ContainsKey(kind) && Rules[kind].ContainsKey(rhs))
            {
                probability *= Rules[kind][rhs].Probability;
            }

            return probability;
        }
     
        private void Count(List<LabeledTree> trees)
        {
            foreach (var tree in trees)
            {
                Count(tree.Root);
            }
        }

        private void Count(LabeledTreeNode parent)
        {
            var kind = parent.ASTNodeType;
            var children = parent.Children;

            var rhs = "";

            foreach (var child in children)
            {
                rhs += $"{child.ASTNodeType} ";
                Count(child);
            }

            rhs = rhs.Trim();

            if(rhs != "")
            {
                IncrementRuleCount(kind, rhs);
            }
        }

        private void IncrementRuleCount(string kind, string rhs)
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