using Microsoft.CodeAnalysis;
using RoseLibML.Util;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.Core;
using MersenneTwister;
using System.Numerics;

namespace RoseLibML
{
    public class TBSampler
    {
        #region Fields

        public BookKeeper BookKeeper { get; set; }
        public LabeledTreePCFGComposer PCFG { get; set; }
        public LabeledTree[] Trees { get; set; }
        public double Alpha { get; set; }
        public double CutProbability { get; set; }

        Writer Writer { get; set; }
        private Config Config { get; set; }

        private List<IProgressListener> listeners = new List<IProgressListener>();

        #endregion


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

        private void Fragmentation(LabeledNode node)
        {
            node.IsFragmentRoot = Randoms.WellBalanced.NextDouble() < CutProbability;

            foreach (var child in node.Children)
            {
                Fragmentation(child);
            }
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
                bookKeeper.IncrementFragmentCount(LabeledNodeType.CalculateFragmentHash(node.GetFragmentString()));
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

            for (int iteration = startIteration; iteration < iterations; iteration++)
            {
                Console.WriteLine($"Iteration: {iteration}");
                UpdateListeners(iteration);

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

                    TypeBlockMove(iteration, typeKV);
                }

                BookKeeper.RemoveZeroNodeTypes();

                if(burnInIterations - 1 < iteration)
                {
                    WriteFragments(fragmentCountTreshold, iteration);
                }

            }

            Console.WriteLine();
            var end = DateTime.Now;
            Console.WriteLine(end.ToString());
            Console.WriteLine("END TRAINING");

            Console.WriteLine($"Time between: {end - begin}");

            Writer.Close();
        }

        private void TypeBlockMove(int iteration, KeyValuePair<LabeledNodeType, List<LabeledNode>> typeKV)
        {
            var typeBlock = CreateTypeBlockAndAdjustCounts(typeKV.Value.ToList(), (short)iteration);

            var typeBlockCardinality = typeBlock.Count;
            var probabilities = CalculateTypeBlockMProbabilities(typeKV.Key, typeBlockCardinality);

            var m = SampleM(probabilities);
            var ones = SampleOnes(typeBlockCardinality, m);
            TraverseSites(typeBlock, ones);
        }


        ulong didNotUpdate = 0;
        ulong updated = 0;
        bool isRedundantUpdate = false;
        private void TraverseSites(List<LabeledNode> typeBlock, List<int> ones)
        {
            isRedundantUpdate = false;

            didNotUpdate = 0;
            updated = 0;

            LabeledNode cutPart1Root = null;
            LabeledNode noncutFullFragmentRoot = null;

            for (int j = typeBlock.Count - 1; j >= 0; j--)
            {
                var node = typeBlock[j];
                var wasFragmentRoot = node.IsFragmentRoot;
                node.IsFragmentRoot = ones[j] == 1;

                if (wasFragmentRoot == node.IsFragmentRoot)
                {
                    didNotUpdate++;
                    isRedundantUpdate = true;
                }
                else
                {
                    updated++;    
                }

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
            //Console.WriteLine($"Shouldn't have: {didNotUpdate} - Should have: {updated} ");
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
                    // If it is a root, then it means that both the part 1 and the part 2
                    // of the full possible fragment are in the bookkeepers counts
                    // and need to be "deducted"
                    if (node.IsFragmentRoot)
                    {
                        BookKeeper.DecrementFragmentCount(node.Type.Part1Fragment);
                        BookKeeper.DecrementFragmentCount(node.Type.Part2Fragment);
                        BookKeeper.DecrementRootCount(node.STInfo);

                        var part1Root = node.Parent.FindFragmentRoot();
                        BookKeeper.DecrementRootCount(part1Root.STInfo);
                    }
                    // If it is not a root, that means only the current full fragment
                    // needs to be "deducted"
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
            node.LastModified = (typeCode: pivot.Type.GetTypeHash(), iteration);

            foreach (var child in node.Children)
            {
                if (child.IsFragmentRoot && child != pivot)
                {
                    child.LastModified = (typeCode: pivot.Type.GetTypeHash(), iteration);
                }
                else
                {
                    SetLastModified(iteration, pivot, child);
                }
            }
        }

        private bool IsNotConflicting(short iteration, LabeledNode pivot, LabeledNode node)
        {

            if (node.LastModified.typeCode == pivot.Type.GetTypeHash() &&
                node.LastModified.iteration == iteration)
            {
                return false;
            }

            foreach (var child in node.Children)
            {
                if (child.IsFragmentRoot && child != pivot)
                {
                    if (child.LastModified.typeCode == pivot.Type.GetTypeHash() &&
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

                if(oldType!= null && !oldType.GetTypeHash().Equals(node.Type.GetTypeHash()) && isRedundantUpdate)
                {
                    //throw new Exception("Did not want to update, but it was necessary!");
                }

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

            var gCalculationInfo = PrepareUnraisedFactors(type);

            for (int m = 0; m <= typeCardinality; m++)
            {
                var gm = CalculateG(m, typeCardinality, gCalculationInfo);
                var combinationsWithoutRepetitions = MathFunctions.CombinationsWithoutRepetition(typeCardinality, m);
                results.Insert(m, combinationsWithoutRepetitions * gm);
            }

            /* Parallel.ForEach(results, new ParallelOptions { MaxDegreeOfParallelism = 2 }, (result, state, index) =>
            {
                var m = (int) index;
                var gm = CalculateGOptimized(m, typeCardinality, gCalculationInfo);
                var combinationsWithoutRepetitions = MathFunctions.CombinationsWithoutRepetition(typeCardinality, m);
                results[m] = combinationsWithoutRepetitions * gm;
            }); */

            List<double> normalizedResults = NormalizeResults(typeCardinality, results);

            return normalizedResults;
        }

        private GCalculationInfo PrepareUnraisedFactors(LabeledNodeType type)
        {
            var gCalculationInfo = new GCalculationInfo();
            var node = BookKeeper.TypeNodes[type].FirstOrDefault();
            var triplet = node.GetRootNodesForTypeFragments();
            gCalculationInfo.Triplet = triplet;


            gCalculationInfo.FfNumeratorUnraised = Alpha * PCFG.CalculateFragmentProbability(triplet.full) + BookKeeper.GetFragmentCount(node.Type.FullFragment);
            gCalculationInfo.P1fNumeratorUnraised = Alpha * PCFG.CalculateFragmentProbability(triplet.part1) + BookKeeper.GetFragmentCount(node.Type.Part1Fragment);
            gCalculationInfo.P2fNumeratorUnraised = Alpha * PCFG.CalculateFragmentProbability(triplet.part2) + BookKeeper.GetFragmentCount(node.Type.Part2Fragment);

            if (!gCalculationInfo.Triplet.full.STInfo.Equals(gCalculationInfo.Triplet.part2.STInfo))
            {
                gCalculationInfo.Ffp1DenominatorUnraised = Alpha + BookKeeper.GetRootCount(triplet.full.STInfo);
                gCalculationInfo.P2DenominatorUnraised = Alpha + BookKeeper.GetRootCount(triplet.part2.STInfo);
            }
            else
            {
                gCalculationInfo.Ffp1p2DenominatorUnraised = Alpha + BookKeeper.GetRootCount(triplet.full.STInfo);
            }

            return gCalculationInfo;
        }

        private double CalculateG(int m, int typeCardinality, GCalculationInfo gCalculationInfo)
        {
            var ffNumeratorLn = MathFunctions.RisingFactorialLn(gCalculationInfo.FfNumeratorUnraised, typeCardinality - m);
            var p1fNumeratorLn = MathFunctions.RisingFactorialLn(gCalculationInfo.P1fNumeratorUnraised, m);
            var p2fNumeratorLn = MathFunctions.RisingFactorialLn(gCalculationInfo.P2fNumeratorUnraised, m);

            var ffp1p2NumeratorLn = ffNumeratorLn + p1fNumeratorLn + p2fNumeratorLn;

            double ffp1p2DenominatorLn;
            if (gCalculationInfo.Triplet.full.STInfo != gCalculationInfo.Triplet.part2.STInfo)
            {
                var ffp1DenominatorLn = MathFunctions.RisingFactorialLn(gCalculationInfo.Ffp1DenominatorUnraised, typeCardinality);
                var p2DenominatorLn = MathFunctions.RisingFactorialLn(gCalculationInfo.P2DenominatorUnraised, m);

                ffp1p2DenominatorLn = ffp1DenominatorLn + p2DenominatorLn;
            }
            else
            {
                ffp1p2DenominatorLn = MathFunctions.RisingFactorialLn(gCalculationInfo.Ffp1p2DenominatorUnraised, typeCardinality + m);
            }

            var resultLn = ffp1p2NumeratorLn - ffp1p2DenominatorLn;
            return Math.Exp(resultLn); 
        }

        private static List<double> NormalizeResults(int typeCardinality, List<double> results)
        {
            var totalSum = 0.0;
            results.ForEach((result) => totalSum += result);
            var normalizationCoefficient = 1 / totalSum;

            var normalizedResults = new List<double>(typeCardinality);
            results.ForEach((result) => normalizedResults.Add(result * normalizationCoefficient));
            return normalizedResults;
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

    internal class GCalculationInfo
    {
        public (LabeledNode full, LabeledNode part1, LabeledNode part2) Triplet { get; set; }

        public double FfNumeratorUnraised { get; set; }
        public double P1fNumeratorUnraised { get; set; }
        public double P2fNumeratorUnraised { get; set; }

        public double Ffp1DenominatorUnraised { get; set; }
        public double P2DenominatorUnraised { get; set; }

        public double Ffp1p2DenominatorUnraised { get; set; }
    }
}
