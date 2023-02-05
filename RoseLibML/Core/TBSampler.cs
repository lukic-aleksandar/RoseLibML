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
                BookKeeper.RecordTreeData(item.tree);

                if(item.index % 100 == 0)
                {
                    Console.WriteLine($"Initialization passed index {item.index}.");
                }
            }

            Writer.Initialize(BookKeeper, Trees);
        }

        private void Fragmentation(LabeledNode node)
        {
            if (!node.IsFixed)
            {
                node.IsFragmentRoot = Randoms.WellBalanced.NextDouble() < CutProbability;
            }

            foreach (var child in node.Children)
            {
                Fragmentation(child);
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
                BookKeeper.RemoveZeroCountRootsAndFragments();

                if (burnInIterations - 1 < iteration)
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
            var typeNodes = typeKV.Value.ToList();
            if (typeNodes != null && typeNodes.Count > 0)
            {
                if (typeNodes[0].IsFixed)
                {
                    return;
                }
            }
            var typeBlock = CreateTypeBlockAndAdjustCounts(typeKV.Value.ToList(), (short)iteration);

            var typeBlockCardinality = typeBlock.Count;
            var probabilities = CalculateTypeBlockMProbabilities(typeKV.Key, typeBlockCardinality);

            var m = SampleM(probabilities);
            var ones = SampleOnes(typeBlockCardinality, m);
            TraverseSites(typeBlock, ones);
        }


        public bool ConfirmSitesAreNotMessedUp()
        {
            var types = BookKeeper.TypeNodes.Keys;
            foreach(var type in types )
            {
                var typeNodes = BookKeeper.TypeNodes[type];
                foreach(var node in typeNodes )
                {
                    var calculatedType = LabeledNode.GetType(node);
                    var innerType = node.Type;
                    var bookkeeperType = type;

                    if(!calculatedType.Equals(innerType) || !calculatedType.Equals(bookkeeperType))
                    {
                        return false;
                    }
                }
            }

            return true;
        }


        #region Conflict checking and type block creation

        public List<LabeledNode> CreateTypeBlockAndAdjustCounts(List<LabeledNode> nodes, short iteration)
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
                        if(part1Root.Parent != null) // Tree roots not recorded as fragment roots. The clause not possible for complete trees, but still, for test purposes.
                        {
                            BookKeeper.DecrementRootCount(part1Root.STInfo);
                        }
                    }
                    // If it is not a root, that means only the current full fragment
                    // needs to be "deducted"
                    else
                    {
                        BookKeeper.DecrementFragmentCount(node.Type.FullFragment);

                        var part1Root = node.Parent.FindFragmentRoot();
                        if (part1Root.Parent != null) // Tree roots not recorded as fragment roots. The clause not possible for complete trees, but still, for test purposes.
                        {
                            BookKeeper.DecrementRootCount(part1Root.STInfo);
                        }
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

        #endregion

        #region Calculation and cut decision
        private List<double> CalculateTypeBlockMProbabilities(LabeledNodeType type, int typeCardinality)
        {
            var results = new List<double>(new double[typeCardinality + 1]);

            var gCalculationInfo = PrepareUnraisedFactors(type);

            Parallel.ForEach(results, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (result, state, index) =>
            {
                var m = (int)index;
                var gm = CalculateG(m, typeCardinality, gCalculationInfo);
                var combinationsWithoutRepetitions = MathFunctions.CombinationsWithoutRepetition(typeCardinality, m);
                results[m] = Math.Exp(Math.Log(combinationsWithoutRepetitions) + Math.Log(gm));
            });

            /*for (int m = 0; m <= typeCardinality; m++)
            {
                var gm = CalculateG(m, typeCardinality, gCalculationInfo);
                var combinationsWithoutRepetitions = MathFunctions.CombinationsWithoutRepetition(typeCardinality, m);
                results[m] = Math.Exp(Math.Log(combinationsWithoutRepetitions) + Math.Log(gm));
            }
            */

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

        #endregion

        #region Site update - a possible update of IsFragment root and fragment types

        private void TraverseSites(List<LabeledNode> typeBlock, List<int> ones)
        {
            List<LabeledNode> cutFullFragmentInnerNodes = null;
            List<LabeledNode> noncutFullFragmentInnerNodes = null;

            for (int j = typeBlock.Count - 1; j >= 0; j--)
            {
                bool isRedundantUpdate = false;

                var node = typeBlock[j];
                var wasFragmentRoot = node.IsFragmentRoot;
                node.IsFragmentRoot = ones[j] == 1;

                var fullFragmentRoot = node.FindFullFragmentRoot();

                if (wasFragmentRoot == node.IsFragmentRoot)
                {
                    isRedundantUpdate = true;
                }

                if (!isRedundantUpdate)
                {

                    var allFullFragmentNodes = node.GetAllFullFragmentNodes();
                    var fullFragmentRoot2 = allFullFragmentNodes[0];
                    var fullFragmentEdgeNodes = node.GetAllFullFragmentLeaves();
                    fullFragmentEdgeNodes.Add(fullFragmentRoot2);

                    var innerFragmentNodes = allFullFragmentNodes
                                                .GetRange(1, allFullFragmentNodes.Count - 1)
                                                .Except(fullFragmentEdgeNodes)
                                                .ToList();

                    if (node.IsFragmentRoot)
                    {
                        if (cutFullFragmentInnerNodes != null && cutFullFragmentInnerNodes.Count > 0)
                        {
                            CopyTypesFromExistingFragmentInnerNodes(cutFullFragmentInnerNodes, innerFragmentNodes, node);
                            UpdateNodesOfFragment(fullFragmentEdgeNodes, node);
                        }
                        else
                        {
                            cutFullFragmentInnerNodes = innerFragmentNodes;
                            UpdateNodesOfFragment(allFullFragmentNodes, node);
                        }
                    }
                    else
                    {
                        if (noncutFullFragmentInnerNodes != null && noncutFullFragmentInnerNodes.Count > 0)
                        {
                            CopyTypesFromExistingFragmentInnerNodes(noncutFullFragmentInnerNodes, innerFragmentNodes, node);
                            UpdateNodesOfFragment(fullFragmentEdgeNodes, node);
                        }
                        else
                        {
                            noncutFullFragmentInnerNodes = innerFragmentNodes;
                            UpdateNodesOfFragment(allFullFragmentNodes, node);
                        }
                    }
                }

                if (ones[j] == 1)
                {
                    BookKeeper.IncrementFragmentCount(node.Type.Part1Fragment);
                    BookKeeper.IncrementFragmentCount(node.Type.Part2Fragment);
                    BookKeeper.IncrementRootCount(node.STInfo);
                    BookKeeper.IncrementRootCount(fullFragmentRoot.STInfo);
                }
                else
                {
                    BookKeeper.IncrementFragmentCount(node.Type.FullFragment);
                    BookKeeper.IncrementRootCount(fullFragmentRoot.STInfo);
                }
            }
        }

        private void UpdateNodesOfFragment(List<LabeledNode> fragmentNodes, LabeledNode pivot)
        {
            foreach (var fragmentNode in fragmentNodes)
            {
                if (fragmentNode != pivot)
                {
                    TryUpdateType(fragmentNode);
                }
            }
        }

        private void CopyTypesFromExistingFragmentInnerNodes(List<LabeledNode> sourceFullFragmentInnerNodes,  List<LabeledNode> destinationFullFragmentInnerNodes, LabeledNode pivot)
        {
            if (destinationFullFragmentInnerNodes.Count != sourceFullFragmentInnerNodes.Count)
            {
                throw new Exception("The 'cut role model list' not the same length as the 'all nodes of a fragment list'");
            }
            for (int i = 0; i < sourceFullFragmentInnerNodes.Count; i++)
            {
                var to = destinationFullFragmentInnerNodes[i];
                if (to == pivot)
                {
                    continue;
                }

                var from = sourceFullFragmentInnerNodes[i];
                if (from.STInfo != to.STInfo || from.Children.Count != to.Children.Count)
                {
                    throw new Exception("Trying to copy, but two nodes are not identical.");
                }

                TryCopyType(from, to);
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
