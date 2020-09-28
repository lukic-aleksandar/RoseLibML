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
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML
{
    public class GibbsSampler
    {
        public BookKeeper BookKeeper { get; set; }
        public LabeledTreePCFGComposer PCFG { get; set; }
        public LabeledTree[] Trees { get; set; }

        public double Alpha { get; set; } = 2;

        public double CutProbability { get; set; } = 0.8;

        public GibbsSampler()
        {
            BookKeeper = new BookKeeper();
        }

        public void Initialize(LabeledTreePCFGComposer pCFG, LabeledTree[] labeledTrees)
        {
            PCFG = pCFG;

            Trees = labeledTrees;

            var bookKeepingResults = new BookKeeper[Trees.Length];
            Parallel.For(0, Trees.Length, (index) =>
            {
                var labeledTree = Trees[index];

                Fragmentation(labeledTree.Root);
                InitializeBookkeeperForTree(index, bookKeepingResults, labeledTree);

                // labeledTree.Serialize();
                if (index % 100 == 0)
                {
                    Console.WriteLine(index);
                }
            });

            foreach (var bookKeepingResult in bookKeepingResults)
            {
                BookKeeper.Merge(bookKeepingResult);
            }
        }

        public void Initialize(string sourcePath, string outDirectory)
        {
            if (Directory.Exists(sourcePath))
            {
                InitializeFromDirectory(sourcePath, outDirectory);
            }
            else
            {
                InitializeFromFile(sourcePath, outDirectory);
            }

            var x = Trees.Select(t => t.Root).ToList(); // Only for debug purposes
            PCFG = new LabeledTreePCFGComposer(Trees.ToList());
            PCFG.CalculateProbabilities();
        }

        private void InitializeFromDirectory(string sourcePath, string outDirectory)
        {
            var directoryInfo = new DirectoryInfo(sourcePath);
            var files = directoryInfo.GetFiles();
            var fileList = files.ToList();

            var bookKeepingResults = new BookKeeper[files.Length];
            Trees = new LabeledTree[files.Length];

            Parallel.For(0, files.Length, (index) =>
            {
                var fileInfo = files[index];

                var labeledTree = CreateLabeledTree(fileInfo, outDirectory);
                Trees[index] = labeledTree;

                InitializeBookkeeperForTree(index, bookKeepingResults, labeledTree);
                // labeledTree.Serialize();

                if (index % 100 == 0)
                {
                    Console.WriteLine(index);
                }
            });

            foreach (var bookKeepingResult in bookKeepingResults)
            {
                BookKeeper.Merge(bookKeepingResult);
            }
        }

        private void InitializeFromFile(string sourcePath, string outDirectory)
        {
            Trees = new LabeledTree[1];
            var fileInfo = new FileInfo(sourcePath);
            var labeledTree = CreateLabeledTree(fileInfo, outDirectory);

            Trees[0] = labeledTree;

            foreach (var child in labeledTree.Root.Children)
            {
                InitializeBookKeeper(child, BookKeeper);
            }
        }

        private void InitializeBookkeeperForTree(int index, BookKeeper[] bookKeepingResults, LabeledTree labeledTree)
        {
            var bookKeeper = new BookKeeper();
            foreach (var child in labeledTree.Root.Children)
            {
                InitializeBookKeeper(child, bookKeeper);
            }

            bookKeepingResults[index] = bookKeeper;
        }

        public void InitializeBookKeeper(LabeledTreeNode node, BookKeeper bookKeeper)
        {
            if (node.IsFragmentRoot)
            {
                bookKeeper.IncrementRootCount(node.ASTNodeType);
                bookKeeper.IncrementFragmentCount(LabeledTreeNode.GetFragmentString(node));
            }

            if (node.CanHaveType)
            {
                bookKeeper.AddNodeType(LabeledTreeNode.GetType(node), node);
            }

            foreach (var child in node.Children)
            {
                InitializeBookKeeper(child, bookKeeper);
            }
        }
        
        public void Train(int iterations)
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

                    if (cnt % 100 == 0)
                    {
                        Console.WriteLine($"Processing type {cnt} of {typeNodes.Count}");
                    }

                    if (!BookKeeper.TypeNodes.ContainsKey(typeKV.Key))
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
            }

            var end = DateTime.Now;
            Console.WriteLine(end);
            Console.WriteLine("END");

            Console.WriteLine($"Time between: {end - begin}");
        }

        Dictionary<LabeledTreeNode, int> traversedSites;
        private void AddToTraversedSites(LabeledTreeNode node)
        {
            
            bool valuefound = traversedSites.TryGetValue(node, out int outValue);
            if (valuefound)
            {
                //throw new Exception("Verovatno je do konflikata.");
                traversedSites[node] = outValue + 1;
            }
            else
            {
                traversedSites[node] = 1;
            }
        }

        int conflicted = 0;
        private void TraverseSites(List<LabeledTreeNode> typeBlock, List<int> ones)
        {
            if(traversedSites != null && traversedSites.Values.Where(v => v > 1).Count() > 0)
            {
                conflicted++;
                Console.WriteLine("Conflicted: " + conflicted);
            }
            traversedSites = new Dictionary<LabeledTreeNode, int>();

            LabeledTreeNode cutPart1Root = null;
            LabeledTreeNode noncutFullFragmentRoot = null;

            for (int j = typeBlock.Count - 1; j >= 0; j--)
            {
                var node = typeBlock[j];
                node.IsFragmentRoot = ones[j] == 1;

                var oldType = node.Type; // Only for debug purposes
                var newType = LabeledTreeNode.GetType(node); // Only for debug purposes

                var fullFragmentRoot = LabeledTreeNode.FindFullFragmentRoot(node);

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
                    BookKeeper.IncrementRootCount(node.ASTNodeType);
                    BookKeeper.IncrementRootCount(LabeledTreeNode.FindFragmentRoot(node.Parent).ASTNodeType);
                }
                else
                {
                    BookKeeper.IncrementFragmentCount(node.Type.FullFragment);
                    BookKeeper.IncrementRootCount(LabeledTreeNode.FindFragmentRoot(node.Parent).ASTNodeType);
                }
            }
        }

        private List<LabeledTreeNode> CreateTypeBlockAndAdjustCounts(List<LabeledTreeNode> nodes, short iteration)
        {
            var typeBlock = new List<LabeledTreeNode>();
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
                        BookKeeper.DecrementRootCount(node.ASTNodeType);

                        var part1Root = LabeledTreeNode.FindFragmentRoot(node.Parent);
                        BookKeeper.DecrementRootCount(part1Root.ASTNodeType);
                    }
                    else
                    {
                        BookKeeper.DecrementFragmentCount(node.Type.FullFragment);

                        var part1Root = LabeledTreeNode.FindFragmentRoot(node.Parent);
                        BookKeeper.DecrementRootCount(part1Root.ASTNodeType);
                    }

                    typeBlock.Add(node);
                }
            }

            return typeBlock;
        }

        private bool CanAddNodeToTypeBlock(short iteration, LabeledTreeNode pivot)
        {
            var fullFragmentRoot = LabeledTreeNode.FindFullFragmentRoot(pivot);

            if(IsNotConflicting(iteration, pivot, fullFragmentRoot))
            {
                SetLastModified(iteration, pivot, fullFragmentRoot);
                return true;
            }

            return false;
        }

        private void SetLastModified(short iteration, LabeledTreeNode pivot, LabeledTreeNode node)
        {
            node.LastModified = (typeCode: pivot.Type.GetMD5HashCode(), iteration);

            foreach (var child in node.Children)
            {
                if (child.IsFragmentRoot && child != pivot)
                {
                    child.LastModified = (typeCode: pivot.Type.GetMD5HashCode(), iteration);
                }
                else
                {
                    SetLastModified(iteration, pivot, child);
                }
            }
        }

        private bool IsNotConflicting(short iteration, LabeledTreeNode pivot, LabeledTreeNode node)
        {

            if (node.LastModified.typeCode == pivot.Type.GetMD5HashCode() &&
                node.LastModified.iteration == iteration)
            {
                return false;
            }

            foreach (var child in node.Children)
            {
                if (child.IsFragmentRoot && child != pivot)
                {
                    if (child.LastModified.typeCode == pivot.Type.GetMD5HashCode() &&
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

        void UpdateTypes(LabeledTreeNode node, LabeledTreeNode pivot)
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

        private bool TryUpdateType(LabeledTreeNode node)
        {
            if (node.CanHaveType)
            {
                AddToTraversedSites(node);
                var oldType = node.Type;
                node.Type = LabeledTreeNode.GetType(node);

                if (oldType != null && BookKeeper.TypeNodes.ContainsKey(oldType))
                {
                    BookKeeper.TypeNodes[oldType].Remove(node);
                    BookKeeper.AddNodeType(node.Type, node);

                    if (BookKeeper.TypeNodes[oldType].Count == 0)
                    {
                        BookKeeper.TypeNodes.Remove(oldType);
                    }
                }

                return true;
            }

            return false;
        }

        private void OptimizedTypeUpdate(LabeledTreeNode from, LabeledTreeNode to, LabeledTreeNode pivot)
        {

            if(from.ASTNodeType != to.ASTNodeType || from.Children.Count != to.Children.Count)
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

        private bool TryCopyType(LabeledTreeNode from, LabeledTreeNode to)
        {
            if (to.CanHaveType)
            {
                AddToTraversedSites(to);
                var oldType = to.Type;
                to.Type = from.Type;

                if (oldType != null && BookKeeper.TypeNodes.ContainsKey(oldType))
                {
                    BookKeeper.TypeNodes[oldType].Remove(to);
                    BookKeeper.AddNodeType(to.Type, to);

                    if (BookKeeper.TypeNodes[oldType].Count == 0)
                    {
                        BookKeeper.TypeNodes.Remove(oldType);
                    }
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
        double CalculateCutProbability(LabeledTreeNodeType type)
        {
            var node = BookKeeper.TypeNodes[type].FirstOrDefault();
            var fragments = LabeledTreeNode.GetFragments(node);

            var fragmentJoin = PosteriorAl(fragments.full, node.Type.FullFragment);
            var part1 = PosteriorAl(fragments.part1, node.Type.Part1Fragment);
            var part2 = PosteriorAl(fragments.part2, node.Type.Part2Fragment);

            var denominator = fragmentJoin + part1 * part2 + Math.Pow(0.1, 10);

            return 1 - (fragmentJoin / denominator); 
        }

        double PosteriorAl(LabeledTreeNode fragment, string fragmentString)
        {
            return (BookKeeper.GetFragmentCount(fragmentString) + Alpha * PCFG.CalculateFragmentProbability(fragment))/
                (BookKeeper.GetRootCount(fragment.ASTNodeType) + Alpha);
        }

        List<double> CalculateTypeBlockProbabilities(LabeledTreeNodeType type)
        {
            var typeCardinality = BookKeeper.TypeNodes[type].Count;
            var gms = new List<double>(typeCardinality);

            for (int m = 0; m <= typeCardinality; m++)
            {
                var node = BookKeeper.TypeNodes[type].FirstOrDefault();
                var fragmets = LabeledTreeNode.GetFragments(node);

                var fullProba = PCFG.CalculateFragmentProbability(fragmets.full) + BookKeeper.GetFragmentCount(node.Type.FullFragment);
                var part1Proba = PCFG.CalculateFragmentProbability(fragmets.part1) + BookKeeper.GetFragmentCount(node.Type.Part1Fragment);
                var part2Proba = PCFG.CalculateFragmentProbability(fragmets.part2) + BookKeeper.GetFragmentCount(node.Type.Part2Fragment);

                var product = RisingFactorial(fullProba, typeCardinality - m) *
                    RisingFactorial(part1Proba, m) / (Alpha + RisingFactorial(BookKeeper.GetRootCount(fragmets.full.ASTNodeType), typeCardinality));

                var product1 = RisingFactorial(part2Proba, m) / (Alpha + RisingFactorial(BookKeeper.GetRootCount(fragmets.part2.ASTNodeType), m));

                gms.Insert(m, product * product1 * Math.Pow(1 / Alpha, (BookKeeper.RootCounts.Count - 2)));
            }

            return gms;
        }

        private double RisingFactorial(double x, double n)
        {
            return SpecialFunctions.Gamma(x + n)/ SpecialFunctions.Gamma(x);
        }

        private LabeledTree CreateLabeledTree(FileInfo sourceInfo, string outDirectory)
        {
            LabeledTree tree = LabeledTree.CreateLabeledTree(sourceInfo, outDirectory);
            TransformTree(tree);
            return tree;
        }

        private void TransformTree(LabeledTree tree)
        {
            LabeledTreeTransformations.Binarize(tree.Root);
            Fragmentation(tree.Root);
        }

        private void Fragmentation(LabeledTreeNode node)
        {
            node.IsFragmentRoot = new Random().NextDouble() < CutProbability;

            foreach (var child in node.Children)
            {
                Fragmentation(child);
            }
        }
    }
}
