using MathNet.Numerics.Distributions;
using Microsoft.CodeAnalysis.CSharp;
using RoseLib;
using RoseLibML.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML
{
    [Serializable]
    public class LabeledTreePCFGComposer 
    { 
        public Dictionary<string, Dictionary<string, PCFGRHSData>> Rules { get; set; }

        [NonSerialized]
        private List<LabeledTree> trees;

        public List<LabeledTree> Trees { get => trees; set => trees = value; }

        public double P { get; set; }

        public LabeledTreePCFGComposer(List<LabeledTree> trees, Config config)
        {
            Rules = new Dictionary<string, Dictionary<string, PCFGRHSData>>();
            Trees = trees;

            P = config.ModelParams.P;
        }

        #region PCFG Calculation

        public void CalculateProbabilitiesLn()
        {
            CountRulesInTrees(Trees);

            foreach (var lhs in Rules.Keys)
            {
                var totalCount = 0.0;

                foreach (var rhs in Rules[lhs].Keys)
                {
                    totalCount += Rules[lhs][rhs].Count;
                }

                foreach (var rhs in Rules[lhs].Keys)
                {
                    Rules[lhs][rhs].ProbabilityLn = Math.Log(Rules[lhs][rhs].Count / totalCount);
                }
            }
        }
     
        private void CountRulesInTrees(List<LabeledTree> trees)
        {
            foreach (var tree in trees)
            {
                CountRulesForNodeAndItsDescendants(tree.Root);
            }
        }

        private void CountRulesForNodeAndItsDescendants(LabeledNode parent)
        {
            var kind = parent.STInfo;
            var children = parent.Children;

            var rhs = "";

            foreach (var child in children)
            {
                rhs += $"{child.STInfo} ";
            }

            rhs = rhs.Trim();

            if(rhs != "")
            {
                IncrementRuleCount(kind, rhs);
            }

            foreach (var child in children)
            {
                CountRulesForNodeAndItsDescendants(child);
            }
        }

        private void IncrementRuleCount(string kind, string rhs)
        {
            if (!Rules.ContainsKey(kind))
            {
                Rules.Add(kind, new Dictionary<string, PCFGRHSData>());
            }

            if (!Rules[kind].ContainsKey(rhs))
            {
                var pcfgNode = new PCFGRHSData(rhs);
                Rules[kind][rhs] = pcfgNode;
            }

            Rules[kind][rhs].Increment();
        }

        #endregion

        #region Fragment proprability - PCFG extension

        public double CalculateFragmentProbability(LabeledNode root)
        {
            var fragmentProbabilityLn = FragmentProbabilityLnFromPCFGRules(root, out int fragmentSize);

            var dist = new Geometric(P);
            var probabilityLn = dist.ProbabilityLn(fragmentSize) + fragmentProbabilityLn;
            return Math.Exp(probabilityLn);
        }

        public double FragmentProbabilityLnFromPCFGRules(LabeledNode node, out int fragmentSize)
        {
            var kind = node.STInfo;
            var children = node.Children;
            fragmentSize = children.Count;

            var rhs = "";
            var probabilityLn = Math.Log(1.0); // 0, a neutral element (addition), to be returned if node has no descendants.

            if (children.Count == 0)
            {
                return probabilityLn;
            }

            foreach (var child in children)
            {
                var childFragmentSize = 0;
                rhs += $"{child.STInfo} ";
                if (!child.IsFragmentRoot)
                {
                    probabilityLn += FragmentProbabilityLnFromPCFGRules(child, out childFragmentSize);
                    fragmentSize += childFragmentSize;
                }
            }

            rhs = rhs.Trim();
            if (Rules.ContainsKey(kind) && Rules[kind].ContainsKey(rhs))
            {
                probabilityLn += Rules[kind][rhs].ProbabilityLn;
            }

            return probabilityLn;
        }

        #endregion

        #region Output

        /*
         
        public SortedDictionary<string, double> GetRulesProbabilities()
        {
            SortedDictionary<string, double> probabilities = new SortedDictionary<string, double>();
            foreach (var lhs in Rules.Keys)
            {
                foreach (var rhs in Rules[lhs].Keys)
                {
                    string rule = CreateRuleString(lhs, rhs);
                    probabilities[rule] = Rules[lhs][rhs].Probability;
                }
            }

            return probabilities;
        }
        */

        /*
        private string CreateRuleString(string lhs, string rhs)
        {
            string leftSide = lhs;

            if (ushort.TryParse(leftSide, out ushort roslynLHS))
            {
                leftSide = ((SyntaxKind)roslynLHS).ToString();
            }
            else if (leftSide.StartsWith("B_"))
            {
                leftSide = "BinarizationNode";
            }

            string rightSide = "";
            foreach (var str in rhs.Split(' '))
            {
                if (ushort.TryParse(str, out ushort roslynRHS))
                {
                    rightSide += $"{(SyntaxKind)roslynRHS} ";
                }
                else if (lhs != "IdentifierToken" && str.StartsWith("B_"))
                {
                    rightSide += "BinarizationNode ";
                }
                else
                {
                    rightSide += $"{str} ";
                }
            }

            return $"{leftSide} --> {rightSide}";
        }
        */

        public void PrintRules(StreamWriter stream)
        {
            foreach (var lhs in Rules.Keys)
            {
                foreach (var rhs in Rules[lhs].Keys)
                {
                    var output = $"{lhs} --> {rhs} {Rules[lhs][rhs].ProbabilityLn}";
                    stream.WriteLine(output);
                }
            }

            stream.Flush();
            stream.Close();
        }

        public void Serialize(string filePath)
        {
            BinaryFormatter b = new BinaryFormatter();
            var fileStream = File.Create(filePath);
            b.Serialize(fileStream, this);
            fileStream.Close();
        }

        public static LabeledTreePCFGComposer Deserialize(string filePath)
        {
            try
            {
                BinaryFormatter b = new BinaryFormatter();
                var fileStream = File.OpenRead(filePath);
                var pCFGComposer = (LabeledTreePCFGComposer)b.Deserialize(fileStream);
                fileStream.Close();
                return pCFGComposer;
            }
            catch
            {

            }

            return null;
        }

        #endregion
    }
}
