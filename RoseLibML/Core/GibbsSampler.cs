using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoseLib;
using RoseLibML;
using RoseLibML.Util;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.Core;
using MersenneTwister;

namespace RoseLibML
{
    public class GibbsSampler
    {
        public BookKeeper BookKeeper { get; set; }
        public LabeledTreePCFGComposer PCFG { get; set; }
        public LabeledTree[] Trees { get; set; }
        public double Alpha { get; set; } = 1;
        public double CutProbability { get; set; } = 0.9;

        Writer Writer { get; set; }

        public GibbsSampler(Writer writer)
        {
            Writer = writer;
            BookKeeper = new BookKeeper();
        }

        public void Initialize(LabeledTreePCFGComposer pCFG, LabeledTree[] labeledTrees)
        {
            PCFG = pCFG;
            Trees = labeledTrees;

            foreach (var item in Trees.Select((tree, index) => new { index, tree }))
            {
                Fragmentation(item.tree.Root);
                AddToBookKeeper(BookKeeper, item.tree);

                if(item.index % 100 == 0)
                {
                    Console.WriteLine($"Initialization passed index {item.index}.");
                }
            }

            Writer.Initialize(BookKeeper, Trees);
        }


        private void AddToBookKeeper(BookKeeper bookKeeper, LabeledTree labeledTree)
        {
            foreach (var child in labeledTree.Root.Children) // Skips the root
            {
                AddToBookKeeper(bookKeeper, child);
            }
        }

        public void AddToBookKeeper(BookKeeper bookKeeper, LabeledNode node)
        {
            if (node.IsFragmentRoot)
            {
                bookKeeper.IncrementRootCount(node.STInfo);
                bookKeeper.IncrementFragmentCount(node.GetFragmentString());
            }

            if (node.CanHaveType)
            {
                bookKeeper.AddNodeType(LabeledNode.GetType(node), node);
            }

            foreach (var child in node.Children)
            {
                AddToBookKeeper(bookKeeper, child);
            }
        }
        
        public void Train(int iterations, int burnInIterations, int fragmentCountTreshold)
        {

            var begin = DateTime.Now;
            Console.WriteLine("START");
            Console.WriteLine(begin);

            for (int i = 0; i < iterations; i++)
            { 
                Console.WriteLine($"Iteration: {i}");

                var typeNodes = BookKeeper.TypeNodes.ToList();
                typeNodes.Shuffle();

                var cnt = 0;
                foreach (var typeKV in typeNodes)
                {
                    cnt++;

                    if (cnt % 1000 == 0)
                    {
                        Console.WriteLine($"Processing type {cnt} of {typeNodes.Count}");
                    }

                    if (!BookKeeper.TypeNodes.ContainsKey(typeKV.Key) || BookKeeper.TypeNodes[typeKV.Key].Count == 0)
                    {
                        continue;
                    }

                    var typeBlock = CreateTypeBlockAndAdjustCounts(typeKV.Value.ToList(), (short)i);

                    var typeBlockCardinality = typeBlock.Count;
                    var cutProbability = CalculateCutProbability(typeKV.Key);
                    var probabilities = CalculateTypeBlockAl(cutProbability, typeBlockCardinality);

                    var m = SampleM(probabilities);
                    var ones = SampleOnes(typeBlockCardinality, m);
                    TraverseSites(typeBlock, ones);
                }

                BookKeeper.RemoveZeroNodeTypes();

                if(burnInIterations - 1 < i)
                {
                    WriteFragments(fragmentCountTreshold, i);
                }
            }

            Console.WriteLine();
            var end = DateTime.Now;
            Console.WriteLine(end);
            Console.WriteLine("END");

            Console.WriteLine($"Time between: {end - begin}");

            Writer.Close();
        }


        // Think about running this functionality in a separate thread. 
        // Concurrent access to the BookKeeper could be a problem.
        private void WriteFragments(int treshold, int iteration)
        {
            Writer.SetIteration(iteration);
            var commonFragments = BookKeeper.FragmentCounts.Where(kvp => kvp.Value > treshold);
            foreach (var fragmentKV in commonFragments)
            {
                var fragmentString = fragmentKV.Key;
                
                Writer.WriteSingleFragment(fragmentString);

            }
        }

        private void TraverseSites(List<LabeledNode> typeBlock, List<int> ones)
        {
            LabeledNode cutPart1Root = null;
            LabeledNode noncutFullFragmentRoot = null;

            for (int j = typeBlock.Count - 1; j >= 0; j--)
            {
                var node = typeBlock[j];
                node.IsFragmentRoot = ones[j] == 1;

                var fullFragmentRoot = node.FindFullFragmentRoot();
                if (node.IsFragmentRoot)
                {
                    if (cutPart1Root != null)
                    {
                        OptimizedTypeUpdate(cutPart1Root, fullFragmentRoot, node);
                    }
                    else
                    {
                        UpdateTypes(fullFragmentRoot, node);
                        cutPart1Root = fullFragmentRoot;
                    }
                }
                else
                {  
                    if(noncutFullFragmentRoot != null)
                    {
                        OptimizedTypeUpdate(noncutFullFragmentRoot, fullFragmentRoot, node);
                    }
                    else
                    {
                        UpdateTypes(fullFragmentRoot, node);
                        noncutFullFragmentRoot = fullFragmentRoot;
                    }
                }

                

                if (ones[j] == 1)
                {
                    BookKeeper.IncrementFragmentCount(node.Type.Part1Fragment);
                    BookKeeper.IncrementFragmentCount(node.Type.Part2Fragment);
                    BookKeeper.IncrementRootCount(node.STInfo);
                    BookKeeper.IncrementRootCount(node.Parent.FindFragmentRoot().STInfo);
                }
                else
                {
                    BookKeeper.IncrementFragmentCount(node.Type.FullFragment);
                    BookKeeper.IncrementRootCount(node.Parent.FindFragmentRoot().STInfo);
                }
            }
        }

        private List<LabeledNode> CreateTypeBlockAndAdjustCounts(List<LabeledNode> nodes, short iteration)
        {
            var typeBlock = new List<LabeledNode>();
            nodes.Shuffle();

            foreach (var node in nodes)
            {
                var canAdd = CanAddNodeToTypeBlock(iteration, node);

                if (canAdd)
                {
                    if (node.IsFragmentRoot)
                    {
                        BookKeeper.DecrementFragmentCount(node.Type.Part1Fragment);
                        BookKeeper.DecrementFragmentCount(node.Type.Part2Fragment);
                        BookKeeper.DecrementRootCount(node.STInfo);

                        var part1Root = node.Parent.FindFragmentRoot();
                        BookKeeper.DecrementRootCount(part1Root.STInfo);
                    }
                    else
                    {
                        BookKeeper.DecrementFragmentCount(node.Type.FullFragment);

                        var part1Root = node.Parent.FindFragmentRoot();
                        BookKeeper.DecrementRootCount(part1Root.STInfo);
                    }

                    typeBlock.Add(node);
                }
            }

            return typeBlock;
        }

        private bool CanAddNodeToTypeBlock(short iteration, LabeledNode pivot)
        {
            var fullFragmentRoot = pivot.FindFullFragmentRoot();

            if (IsNotConflicting(iteration, pivot, fullFragmentRoot))
            {
                SetLastModified(iteration, pivot, fullFragmentRoot);
                return true;
            }

            return false;
        }

        private void SetLastModified(short iteration, LabeledNode pivot, LabeledNode node)
        {
            node.LastModified = (typeCode: pivot.Type.GetQuasiUniqueRepresentation(), iteration);

            foreach (var child in node.Children)
            {
                if (child.IsFragmentRoot && child != pivot)
                {
                    child.LastModified = (typeCode: pivot.Type.GetQuasiUniqueRepresentation(), iteration);
                }
                else
                {
                    SetLastModified(iteration, pivot, child);
                }
            }
        }

        private bool IsNotConflicting(short iteration, LabeledNode pivot, LabeledNode node)
        {

            if (node.LastModified.typeCode == pivot.Type.GetQuasiUniqueRepresentation() &&
                node.LastModified.iteration == iteration)
            {
                return false;
            }

            foreach (var child in node.Children)
            {
                if (child.IsFragmentRoot && child != pivot)
                {
                    if (child.LastModified.typeCode == pivot.Type.GetQuasiUniqueRepresentation() &&
                        child.LastModified.iteration == iteration)
                    {
                        return false;
                    }
                }
                else
                {
                    if (!IsNotConflicting(iteration, pivot, child))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        void UpdateTypes(LabeledNode node, LabeledNode pivot)
        {
            TryUpdateType(node);

            foreach (var child in node.Children)
            {
                if (child.IsFragmentRoot && child != pivot)
                {
                    TryUpdateType(child);
                }
                else
                {
                    UpdateTypes(child, pivot);
                }
            }
        }

        private bool TryUpdateType(LabeledNode node)
        {
            if (node.CanHaveType)
            {
                var oldType = node.Type;
                node.Type = LabeledNode.GetType(node);

                if (oldType != null && BookKeeper.TypeNodes.ContainsKey(oldType))
                {
                    BookKeeper.RemoveNodeType(oldType, node); 
                    BookKeeper.AddNodeType(node.Type, node);
                }

                return true;
            }

            return false;
        }

        private void OptimizedTypeUpdate(LabeledNode from, LabeledNode to, LabeledNode pivot)
        {

            if(from.STInfo != to.STInfo || from.Children.Count != to.Children.Count)
            {
                throw new Exception("Trying to copy, but two nodes are not identical.");
            }

            // If it's not a pivot, and can have a type
            // it should get a new type
            if(to != pivot && to.CanHaveType)
            {
                // If it's a root (being a root of the current fragment, or a leaf)
                // Update it by calculating the new type (because it can't be copied from existing)
                if (to.IsFragmentRoot)
                {
                    TryUpdateType(to);
                }
                // If it's not a root, then it's inside of the current fragment, and
                // the type of corresponding can be copied
                else
                {
                    TryCopyType(from, to);
                }
            }

            for (int i = 0; i < to.Children.Count; i++)
            {
                var toChild = to.Children[i];
                var fromChild = from.Children[i];

                // If a child can have type, the update process should continue
                if(toChild.CanHaveType)
                {
                    // If a child is fragment root (but not a pivot, because the pivot should be skipped),
                    // Update it by calculating the new type (because it can't be copied from existing)
                    if (toChild.IsFragmentRoot && toChild != pivot)
                    {
                        TryUpdateType(toChild);
                    }
                    // Else, recursion
                    else
                    {
                        OptimizedTypeUpdate(fromChild, toChild, pivot);
                    }
                }
            }
        }

        private bool TryCopyType(LabeledNode from, LabeledNode to)
        {
            if (to.CanHaveType)
            {
                var oldType = to.Type;
                to.Type = from.Type;

                if (oldType != null && BookKeeper.TypeNodes.ContainsKey(oldType))
                {
                    BookKeeper.RemoveNodeType(oldType, to);
                    BookKeeper.AddNodeType(to.Type, to);
                }

                return true;
            }

            return false;
        }

        private List<double> CalculateMProbabilities(List<double> probabilities, int typeBlockCardinality)
        {
            for (int i = 0; i < probabilities.Count; i++)
            {
                probabilities[i] *= SpecialFunctions.Binomial(typeBlockCardinality, i);
            }

            var list_exp = probabilities.Select(Math.Exp);
            var sum_z_exp = list_exp.Sum();

            return list_exp.Select(i => i / sum_z_exp).ToList();
        }

        public int SampleM(List<double> list)
        {
            var randomNumber = new Random().NextDouble();

            var sum = 0.0;
            for (int i = 0; i < list.Count; i++)
            {
                sum += list[i];

                if (sum >= randomNumber)
                {
                    return i;
                }
            }

            return 0;
        }

        public List<int> SampleOnes(int numberOfElements, int numberOfOnes)
        {
            var array = new int[numberOfElements];

            for (int i = 0; i < numberOfOnes; i++)
            {
                array[i] = 1;
            }

            var list = array.ToList();
            list.Shuffle();

            return list;
        }

        // Allemanis paper implementation gives cut probability for one node.
        // To calculate for x nodes, binomial distribution is used
        List<double> CalculateTypeBlockAl(double p, int typeCardinality)
        {
            var probabilities = new List<double>();
            var distribution = new Binomial(p, typeCardinality);
          
            for (int m = 0; m <= typeCardinality; m++)
            {
                probabilities.Add(distribution.Probability(m));
            }

            return probabilities;
        }

        // Implementation based on Allemanis paper
        double CalculateCutProbability(LabeledNodeType type)
        {
            var node = BookKeeper.TypeNodes[type].FirstOrDefault();
            var fragments = node.GetFragments();

            var fragmentJoin = PosteriorAl(fragments.full, node.Type.FullFragment);
            var part1 = PosteriorAl(fragments.part1, node.Type.Part1Fragment);
            var part2 = PosteriorAl(fragments.part2, node.Type.Part2Fragment);

            var denominator = fragmentJoin + part1 * part2 + Math.Pow(0.1, 10);

            return 1 - (fragmentJoin / denominator); 
        }

        double PosteriorAl(LabeledNode fragment, string fragmentString)
        {
            return (BookKeeper.GetFragmentCount(fragmentString) + Alpha * PCFG.CalculateFragmentProbability(fragment))/
                (BookKeeper.GetRootCount(fragment.STInfo) + Alpha);
        }

        List<double> CalculateTypeBlockProbabilities(LabeledNodeType type)
        {
            var typeCardinality = BookKeeper.TypeNodes[type].Count;
            var gms = new List<double>(typeCardinality);

            for (int m = 0; m <= typeCardinality; m++)
            {
                var node = BookKeeper.TypeNodes[type].FirstOrDefault();
                var fragmets = node.GetFragments();

                var fullProba = PCFG.CalculateFragmentProbability(fragmets.full) + BookKeeper.GetFragmentCount(node.Type.FullFragment);
                var part1Proba = PCFG.CalculateFragmentProbability(fragmets.part1) + BookKeeper.GetFragmentCount(node.Type.Part1Fragment);
                var part2Proba = PCFG.CalculateFragmentProbability(fragmets.part2) + BookKeeper.GetFragmentCount(node.Type.Part2Fragment);

                var product = RisingFactorial(fullProba, typeCardinality - m) *
                    RisingFactorial(part1Proba, m) / (Alpha + RisingFactorial(BookKeeper.GetRootCount(fragmets.full.STInfo), typeCardinality));

                var product1 = RisingFactorial(part2Proba, m) / (Alpha + RisingFactorial(BookKeeper.GetRootCount(fragmets.part2.STInfo), m));

                gms.Insert(m, product * product1 * Math.Pow(1 / Alpha, (BookKeeper.RootCounts.Count - 2)));
            }

            return gms;
        }

        private double RisingFactorial(double x, double n)
        {
            return SpecialFunctions.Gamma(x + n)/ SpecialFunctions.Gamma(x);
        }

        private void Fragmentation(LabeledNode node)
        {
            node.IsFragmentRoot = Randoms.WellBalanced.NextDouble() < CutProbability;

            foreach (var child in node.Children)
            {
                Fragmentation(child);
            }
        }
    }
}
