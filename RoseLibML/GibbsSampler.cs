using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoseLib;
using RoseLibML.Util;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public void Initialize(string sourcePath, string outDirectory)
        {
            if (Directory.Exists(sourcePath))
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
                    Binarize(labeledTree.Root);
                    var bookKeeper = new BookKeeper();

                    foreach (var child in labeledTree.Root.Children)
                    {
                        InitializeBookKeeper(child, bookKeeper);
                    }

                    bookKeepingResults[index] = bookKeeper;
                    labeledTree.Serialize();
                    Trees[index] = labeledTree;

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
            else
            {
                Trees = new LabeledTree[1];
                var fileInfo = new FileInfo(sourcePath);
                var labeledTree = CreateLabeledTree(fileInfo, outDirectory);
                Binarize(labeledTree.Root);

                Trees[0] = labeledTree;

                foreach (var child in labeledTree.Root.Children)
                {
                    InitializeBookKeeper(child, BookKeeper);
                }
            }

            var x = Trees.Select(t => t.Root).ToList();
            PCFG = new LabeledTreePCFGComposer(Trees.ToList());
            PCFG.CalculateProbabilities();
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
                node.Type = LabeledTreeNode.GetType(node);
                bookKeeper.AddNodeType(node.Type, node);
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

                    for (int j = typeBlock.Count - 1; j >= 0; j--)
                    {
                        var node = typeBlock[j];
                        node.IsFragmentRoot = ones[j] == 1;
                        var oldType = node.Type;
                        var newType = LabeledTreeNode.GetType(node);

                        var isFragmentRoot = node.IsFragmentRoot;
                        node.IsFragmentRoot = false;
                        var fullFragmentRoot = LabeledTreeNode.FindFragmentRoot(node);
                        node.IsFragmentRoot = isFragmentRoot;

                        UpdateTypes(fullFragmentRoot, node);

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
            }

            var end = DateTime.Now;
            Console.WriteLine(end);
            Console.WriteLine("END");

            Console.WriteLine($"Time between: {end - begin}");
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

        private bool CanAddNodeToTypeBlock(short iteration, LabeledTreeNode fragmentRoot)
        {
            if(CheckForConflicts(iteration, fragmentRoot, fragmentRoot))
            {
                SetLastModified(iteration, fragmentRoot, fragmentRoot);
                return false;
            }

            return true;
        }

        private void SetLastModified(short iteration, LabeledTreeNode fragmentRoot, LabeledTreeNode node)
        {
            node.LastModified = (typeCode: fragmentRoot.Type.GetHashCode(), iteration: iteration);

            foreach (var child in node.Children)
            {
                if (child.IsFragmentRoot)
                {
                    child.LastModified = (typeCode: fragmentRoot.Type.GetHashCode(), iteration: iteration);
                }
                else
                {
                    SetLastModified(iteration, fragmentRoot, child);
                }
            }
        }

        private bool CheckForConflicts(short iteration, LabeledTreeNode fragmentRoot, LabeledTreeNode node)
        {
            var result = true;

            if(fragmentRoot.Type == node.Type && 
                node.LastModified.typeCode == node.Type.GetHashCode() && 
                node.LastModified.iteration == iteration)
            {
                return false;
            }

            foreach (var child in node.Children)
            {
                if (child.IsFragmentRoot)
                {
                    if (fragmentRoot.Type == node.Type &&
                        node.LastModified.typeCode == node.Type.GetHashCode() &&
                        node.LastModified.iteration == iteration)
                    {
                        return false;
                    }
                }
                else
                {
                    result = CheckForConflicts(iteration, fragmentRoot, child);
                }
            }

            return result;
        }

        void UpdateTypes(LabeledTreeNode node, LabeledTreeNode pivot)
        {
            TryUpdateType(node);

            foreach (var child in node.Children)
            {
                if (node.IsFragmentRoot && child != pivot)
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

        double CalculateCutProbability(LabeledTreeNodeType type)
        {
            var node = BookKeeper.TypeNodes[type].FirstOrDefault();
            var fragments = LabeledTreeNode.GetFragments(node);

            var fragmentJoin = PosteriorAl(fragments.full, node.Type.FullFragment);
            var part1 = PosteriorAl(fragments.part1, node.Type.Part1Fragment);
            var part2 = PosteriorAl(fragments.part2, node.Type.Part2Fragment);

            var denominator = fragmentJoin + part1 * part2;

            if(denominator == 0)
            {
                return 0.000001;
            }
            else
            {
                return 1 - (fragmentJoin / denominator);
            }
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
            using (StreamReader sr = new StreamReader(sourceInfo.FullName))
            {
                var source = sr.ReadToEnd();
                var tree = CSharpSyntaxTree.ParseText(source);

                var labeledTree = new LabeledTree();
                labeledTree.SourceFilePath = sourceInfo.FullName;
                labeledTree.FilePath = $"{outDirectory}/{sourceInfo.Name}.bin";
                labeledTree.Root = CreateLabeledNode(tree.GetRoot());
                labeledTree.Root.IsFragmentRoot = true;
                labeledTree.Root.CanHaveType = false;

                return labeledTree;
            }
        }

        private void Binarize(LabeledTreeNode parent)
        {
            if(parent.Children.Count > 2)
            {
                var firstChild = parent.Children.FirstOrDefault();
                var restOfChildren = parent.Children.ToList();
                restOfChildren.RemoveAt(0);
                parent.Children.Clear();

                var tempNode = new LabeledTreeNode()
                {
                    ASTNodeType = "BinTempNode",
                    IsFragmentRoot = new Random().NextDouble() < CutProbability
                };

                parent.AddChild(firstChild);
                parent.AddChild(tempNode);

                foreach (var child in restOfChildren)
                {
                    tempNode.AddChild(child);
                }

                Binarize(firstChild);
                Binarize(tempNode);
            }
            else
            {
                foreach (var child in parent.Children)
                {
                    Binarize(child);
                }
            }
       
        }

        private LabeledTreeNode CreateLabeledNode(SyntaxNode parent)
        {
            var children = parent.ChildNodesAndTokens();
            var treeNode = new LabeledTreeNode();
         
            treeNode.ASTNodeType = parent.Kind().ToString();
            treeNode.IsFragmentRoot = new Random().NextDouble() < CutProbability;


            foreach (var child in children)
            {
                if (child.IsNode)
                {
                    treeNode.AddChild(CreateLabeledNode(child.AsNode()));
                }
                else if (child.IsToken)
                {
                    var tokenNode = new LabeledTreeNode();
                    tokenNode.ASTNodeType = child.AsToken()
                                            .Kind()
                                            .ToString();
                    tokenNode.CanHaveType = false;
                    treeNode.AddChild(tokenNode);

                    if (child.AsToken().Kind() == SyntaxKind.IdentifierToken)
                    {   
                        var leaf = new LabeledTreeNode();
                        leaf.ASTNodeType = child.AsToken().ValueText;
                        leaf.CanHaveType = false;
                        tokenNode.CanHaveType = true;
                        tokenNode.AddChild(leaf);
                    }
                }
            }

            return treeNode;
        }
    }
}
