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
using System.Numerics;
using System.Runtime.CompilerServices;

namespace RoseLibML
{
    public class TBSampler
    {
        public BookKeeper BookKeeper { get; set; }
        public LabeledTreePCFGComposer PCFG { get; set; }
        public LabeledTree[] Trees { get; set; }
        public double Alpha { get; set; }
        public double CutProbability { get; set; }

        Writer Writer { get; set; }
        private Config Config { get; set; }

        private List<IProgressListener> listeners = new List<IProgressListener>();

        public TBSampler(Writer writer, Config config)
        {
            Writer = writer;
            BookKeeper = new BookKeeper();

            Alpha = config.ModelParams.Alpha;
            CutProbability = config.ModelParams.CutProbability;

            Config = config;
        }

        public void Initialize(LabeledTreePCFGComposer pCFG, LabeledTree[] labeledTrees, bool modelLoaded)
        {
            PCFG = pCFG;
            Trees = labeledTrees;

            foreach (var item in Trees.Select((tree, index) => new { index, tree }))
            {
                if (!modelLoaded) // Model loaded. Skip fragmentation.
                {
                    Fragmentation(item.tree.Root);
                }
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
        
        public void Train()
        {
            int startIteration = Config.RunParams.StartIteration;
            int iterations = Config.RunParams.TotalIterations;
            int burnInIterations = Config.RunParams.BurnIn;
            int fragmentCountTreshold = Config.RunParams.Threshold;

            var begin = DateTime.Now;
            Console.WriteLine("START TRAINING");
            Console.WriteLine(begin.ToString());

            for (int i = startIteration; i < iterations; i++)
            {
                Console.WriteLine($"Iteration: {i}");
                UpdateListeners(i);

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
                    var probabilities = CalculateTypeBlockMProbabilities(typeKV.Key, typeBlockCardinality);

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
            Console.WriteLine(end.ToString());
            Console.WriteLine("END TRAINING");

            Console.WriteLine($"Time between: {end - begin}");

            Writer.Close();
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

        #region Conflict checking and type block creation

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

        #endregion

        #region Type update
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

        #endregion

       

        public int SampleM(List<double> list)
        {
            var randomNumber = Randoms.WellBalanced.NextDouble();

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

        private List<double> CalculateTypeBlockMProbabilities(LabeledNodeType type, int typeCardinality)
        {
            var results = new List<double>(new double[typeCardinality]);

            var gCalculationInfo = new GCalculationInfo();

            var node = BookKeeper.TypeNodes[type].FirstOrDefault();
            var triplet = node.GetRootNodesForTypeFragments();
            gCalculationInfo.Triplet = triplet;

            gCalculationInfo.FfNumerator = Alpha * PCFG.CalculateFragmentProbability(triplet.full) + BookKeeper.GetFragmentCount(node.Type.FullFragment);
            gCalculationInfo.P1fNumerator = Alpha * PCFG.CalculateFragmentProbability(triplet.part1) + BookKeeper.GetFragmentCount(node.Type.Part1Fragment);
            gCalculationInfo.P2fNumerator = Alpha * PCFG.CalculateFragmentProbability(triplet.part2) + BookKeeper.GetFragmentCount(node.Type.Part2Fragment);

            gCalculationInfo.FfRootCount = BookKeeper.GetRootCount(triplet.full.STInfo);
            gCalculationInfo.P2fRootCount = BookKeeper.GetRootCount(triplet.part2.STInfo);

            for (int m = 0; m <= typeCardinality; m++)
            {
                var gm = CalculateGOptimized(m, typeCardinality, gCalculationInfo);
                var combinationsWithoutRepetitions = SpecialFunctions.Factorial(typeCardinality) / ((SpecialFunctions.Factorial(typeCardinality - m) * SpecialFunctions.Factorial(m)));
                results.Insert(m, combinationsWithoutRepetitions * gm);
            }

            /* Parallel.ForEach(results, new ParallelOptions { MaxDegreeOfParallelism = 2 }, (result, state, index) =>
            {
                var m = (int) index;
                var gm = CalculateGOptimized(m, typeCardinality, gCalculationInfo);
                var combinationsWithoutRepetitions = SpecialFunctions.Factorial(typeCardinality) / ((SpecialFunctions.Factorial(typeCardinality - m) * SpecialFunctions.Factorial(m)));
                results[m] = combinationsWithoutRepetitions * gm;
            }); */

            var totalSum = 0.0;
            results.ForEach((result) => totalSum += result);
            var normalizationCoefficient = 1 / totalSum;

            var normalizedResults = new List<double>(typeCardinality);
            results.ForEach((result) => normalizedResults.Add(result * normalizationCoefficient));

            return normalizedResults;
        }

        private double CalculateGOptimized(int m, int typeCardinality, GCalculationInfo gCalculationInfo)
        {
            var ffNumeratorRaised = CalculateRisingFactorial(gCalculationInfo.FfNumerator, typeCardinality - m);
            var p1fNumeratorRaised = CalculateRisingFactorial(gCalculationInfo.P1fNumerator, m);
            var p2fNumeratorRaised = CalculateRisingFactorial(gCalculationInfo.P2fNumerator, m);

            if (gCalculationInfo.Triplet.full.STInfo != gCalculationInfo.Triplet.part2.STInfo)
            {
                var ffp1fDenominator = CalculateRisingFactorial(Alpha + gCalculationInfo.FfRootCount, typeCardinality);
                var p2fDenominator = CalculateRisingFactorial(Alpha + gCalculationInfo.P2fRootCount, m);

                var ffp1fNumerator = Multiply(ffNumeratorRaised, p1fNumeratorRaised);

                var ffp1fResult = Divide(ffp1fNumerator, ffp1fDenominator);
                var p2fResult = Divide(p2fNumeratorRaised, p2fDenominator);
                return ffp1fResult * p2fResult;
            }
            else
            {
                var ffp1fp2fNumerator = Multiply(Multiply(ffNumeratorRaised, p1fNumeratorRaised),p2fNumeratorRaised);
                var ffp1fp2fDenominator = CalculateRisingFactorial(Alpha + gCalculationInfo.FfRootCount, typeCardinality + m);
                return Divide(ffp1fp2fNumerator, ffp1fp2fDenominator);
            }
        }

        private (double, BigInteger?) CalculateRisingFactorial(double x, double n)
        {
            if(x+n <= 19)
            {
                var result = RisingFactorial(x, n);
                return (result, null);
            }
            
                return (0, RisingFactorialBIOptimized((int) Math.Round(x), 0, ((int) Math.Round(n) - 1)));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (double, BigInteger?) Add(double value, (double, BigInteger?) tuple)
        {
            return tuple.Item2 == null ? (value + tuple.Item1, null) : (0.0, new BigInteger(value) + tuple.Item2);
        }

        static long countOrdinary = 0;
        static long countBI = 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (double, BigInteger?) Multiply((double, BigInteger?) first, (double, BigInteger?) second)
        {
            if (first.Item2 == null && second.Item2 == null)
            {
                var result = first.Item1 * second.Item1;
                if (result.IsFinite()) { countOrdinary++; return (result, null); }
                else
                {
                    countBI++;
                    return (0, new BigInteger(Math.Round(first.Item1)) * new BigInteger(Math.Round(second.Item1)));
                }
            }

            countBI++;
            var firstBI = first.Item2 != null ? first.Item2 : new BigInteger(first.Item1);
            var secondBI = second.Item2 != null ? second.Item2 : new BigInteger(second.Item1);

            return (0, firstBI * secondBI);
        }


        static long countOrdinaryDiv = 0;
        static long countBIDiv = 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double Divide((double, BigInteger?) numerator, (double, BigInteger?) denominator)
        {
            if(numerator.Item2 != null && denominator.Item2 != null)
            {
                countOrdinaryDiv++;
                return numerator.Item1 / denominator.Item1;
            }
            else
            {
                countBIDiv++;
                var numeratorBI = numerator.Item2 != null ? numerator.Item2 : new BigInteger(numerator.Item1);
                var denominatorBI = denominator.Item2 != null ? denominator.Item2 : new BigInteger(denominator.Item1);

                return Math.Exp(BigInteger.Log((BigInteger) numeratorBI) - BigInteger.Log((BigInteger) denominatorBI));
            }
        }


        private double RisingFactorial(double x, double n)
        {
            if((x+n) > 19)
            {
                throw new OverflowException();
            }

            x = x < 1 ? 1 : x;
            var numerator = SpecialFunctions.Gamma(x + n);
            var denominator = SpecialFunctions.Gamma(x);

            if(!numerator.IsFinite() || !denominator.IsFinite())
            {
                throw new OverflowException();
            }

            return numerator / denominator;
        }

        public static BigInteger RisingFactorialBIOptimized(int x, int startn, int endn)
        {
            if (startn >= endn)
            {
                return new BigInteger(x + startn);
            }

            var leftstartn = startn;
            var rightendn = endn;

            var middlen = (startn + endn) / 2.0;
            int leftendn;
            int rightstartn;
            if (middlen - (int)middlen == 0)
            {
                leftendn = (int)middlen;
                rightstartn = (int)middlen + 1;
            }
            else
            {
                leftendn = (int)Math.Floor(middlen);
                rightstartn = (int)Math.Ceiling(middlen);
            }

            var leftResult = RisingFactorialBIOptimized(x, leftstartn, leftendn);
            var rightResult = RisingFactorialBIOptimized(x, rightstartn, rightendn);

            return leftResult * rightResult;
        }

        private void Fragmentation(LabeledNode node)
        {
            node.IsFragmentRoot = Randoms.WellBalanced.NextDouble() < CutProbability;

            foreach (var child in node.Children)
            {
                Fragmentation(child);
            }
        }


        public void WriteFragments(int treshold, int iteration)
        {
            Writer.SetIteration(iteration);
            var commonFragments = BookKeeper.FragmentCounts.Where(kvp => kvp.Value > treshold);
            foreach (var fragmentKV in commonFragments)
            {
                var fragmentString = fragmentKV.Key;

                Writer.WriteSingleFragment(fragmentString);

            }
        }

        public void AddListener(IProgressListener listener)
        {
            listeners.Add(listener);
        }

        public void UpdateListeners(int iteration)
        {
            foreach(var listener in listeners)
            {
                listener.Update(iteration);
            }
        }
    }

    class GCalculationInfo
    {
        public (LabeledNode full, LabeledNode part1, LabeledNode part2) Triplet { get; set; }


        public double FfNumerator { get; set; }
        public double P1fNumerator { get; set; }
        public double P2fNumerator { get; set; }

        public int FfRootCount { get; set; }
        public int P2fRootCount { get; set; }
    }


}
