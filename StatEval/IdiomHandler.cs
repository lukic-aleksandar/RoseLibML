using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS.CSTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StatEval
{
    public class IdiomHandler
    {
        public HashSet<string> FinalTrainingIdioms = new HashSet<string>();

        public Dictionary<string, List<LabeledNode>> IntermediateTrainingIdiomRoots = new Dictionary<string, List<LabeledNode>>(); // Count (Needs to get pruned at the end of each "cycle")
        public Dictionary<string, int> TrainingIdiomLength = new Dictionary<string, int>(); // Length
        
        public Dictionary<string, List<LabeledNode>> IdenticalTrainingSubtreeTestRoots = new Dictionary<string, List<LabeledNode>>();
        public List<string> IdiomsFoundInAsIdenticalSubreesInTest = new List<string>();

        public List<LabeledTree>? TrainingLabeledTrees { get; set; } 
        
        public static IdiomHandler CreateEmptyIdiomHandler()
        {
            return new IdiomHandler(); 
        }

        private IdiomHandler() { }
        
        public IdiomHandler(List<LabeledTree> trainingLabeledTrees)
        {
            this.TrainingLabeledTrees = trainingLabeledTrees;
        }

        public void SortOutIdiomsInTrainingSet(int countThreshold, int lengthThreshold)
        {
            if (TrainingLabeledTrees == null)
            {
                throw new Exception("Training labeled trees are not set!");
            }

            foreach (var labeledTree in TrainingLabeledTrees)
            {
                FindTreeIdioms(labeledTree, labeledTree.Root);
            }

            foreach (var idiom in IntermediateTrainingIdiomRoots.Keys)
            {
                var isCountAboveThreshold = IntermediateTrainingIdiomRoots[idiom].Count >= countThreshold;
                var isLengthAboveThreshold = TrainingIdiomLength[idiom] >= lengthThreshold;

                if(isCountAboveThreshold && isLengthAboveThreshold)
                {
                    FinalTrainingIdioms.Add(idiom);
                }
            }
        }

        public double CalculateRecall(List<LabeledTree> testLabeledTrees)
        {
            if (FinalTrainingIdioms == null || FinalTrainingIdioms.Count == 0)
            {
                throw new Exception("Call the function SortOutIdiomsInTrainingSet to populate the structures and be able to compare.");
            }

            IdenticalTrainingSubtreeTestRoots.Clear();

            foreach (var idiom in FinalTrainingIdioms)
            {
                Parallel.ForEach(testLabeledTrees, (tlt) => FindIdenticalSubtrees(idiom, tlt));

                if(IdenticalTrainingSubtreeTestRoots.Count() > 0)
                {
                    IdiomsFoundInAsIdenticalSubreesInTest.Add(idiom);
                    IdenticalTrainingSubtreeTestRoots.Clear();
                }
            }

            return IdiomsFoundInAsIdenticalSubreesInTest.Count / (double) FinalTrainingIdioms.Count;
        }

        public double CalculateCoverage(List<LabeledTree> testLabeledTrees)
        {
            // Clear any existing marks
            Parallel.ForEach(testLabeledTrees, (tlt) => ClearAnyNodeMarks(tlt.Root));
            // Find identical subtrees (take a look at the method called above)
            // And mark them! Change the method! :D
            foreach (var idiom in FinalTrainingIdioms)
            {
                Parallel.ForEach(testLabeledTrees, (tlt) => FindIdenticalSubtrees(idiom, tlt, true));
            }

            // count all marked nodes (non binary nodes, I guess)
            // count all nodes (non binary nodes, I guess)
            // divide the two to calculate the percentage
            var totalCount = 0;
            var markedCount = 0;
            foreach(var tlt in testLabeledTrees)
            {
                totalCount += CountAllNonBinarySubtreeNodes(tlt.Root);
                markedCount += CountAllMarkedNonBinarySubtreeNodes(tlt.Root);
            }

            return markedCount / (double)totalCount; 
        }

        private void ClearAnyNodeMarks(LabeledNode node)
        {
            if (node == null) { return; }
            node.IdiomMark = null;

            if (node.Children != null)
            { 
                foreach(var child in node.Children)
                {
                    ClearAnyNodeMarks(child);
                }
            }
        }

        private void FindTreeIdioms(LabeledTree tree, LabeledNode node)
        {
            if (node.IsTreeRoot() || (node.IsFragmentRoot && !node.IsTreeLeaf))
            {
                var idiomString = node.GetFragmentString();
                AddToIdiomRootsDict(node, idiomString);
            }

            foreach (var child in node.Children)
            {
                FindTreeIdioms(tree, child);
            }
        }

        private void AddToIdiomRootsDict(LabeledNode node, string idiomString)
        {
            if (!IntermediateTrainingIdiomRoots.ContainsKey(idiomString))
            {
                IntermediateTrainingIdiomRoots[idiomString] = new List<LabeledNode>();
                TrainingIdiomLength.Add(idiomString, CalculateIdiomLenght(idiomString));
            }
            IntermediateTrainingIdiomRoots[idiomString].Add(node);
        }


        private object _addLock = new object();
        private void FindIdenticalSubtrees(string idiom, LabeledTree labeledTree, bool markIfIdentical = false)
        {
            var exampleSubtreeRoot = CSNode.CreateSubtreeFromIdiom(idiom);//TrainingIdiomRoots[idiom].FirstOrDefault();
            if (exampleSubtreeRoot == null)
            {
                throw new ArgumentException("Provided with an idiom not present in idiom roots dictionary");
            }

            var foundNodes = FindNodesWithSTInfo(exampleSubtreeRoot.STInfo, labeledTree);
            foreach (var node in foundNodes)
            {
                if (RootIdenticalSubtrees(exampleSubtreeRoot, node, markIfIdentical))
                {
                    lock (_addLock)
                    {
                        if (!IdenticalTrainingSubtreeTestRoots.ContainsKey(idiom))
                        {
                            IdenticalTrainingSubtreeTestRoots[idiom] = new List<LabeledNode>();
                        }
                        IdenticalTrainingSubtreeTestRoots[idiom].Add(node);
                    }
                }
            }
        }

        private bool RootIdenticalSubtrees(LabeledNode exampleRootNode, LabeledNode testedRootNode, bool markIfIdentical = false)
        {
            if (exampleRootNode.Children.Count != testedRootNode.Children.Count)
            {
                return false;
            }

            Queue<LabeledNode> exampleQueue = new Queue<LabeledNode>(exampleRootNode.Children);
            Queue<LabeledNode> testedQueue = new Queue<LabeledNode>(testedRootNode.Children);

            List<LabeledNode> allIdiomMatchingNodes = new List<LabeledNode>();

            if (markIfIdentical)
            {
                // The roots are always the same...
                allIdiomMatchingNodes.Add(testedRootNode);
            }

            while (exampleQueue.Count > 0)
            {
                var exampleDescendant = exampleQueue.Dequeue();
                var testedDescendant = testedQueue.Dequeue();

                if (exampleDescendant.STInfo.Trim() != testedDescendant.STInfo.Trim())
                {
                    return false;
                }
                else
                {
                    // If the STInfos are the same, then we can add them, no need to check the children.
                    // If the subtrees are not the same, no permanent marks left, regardlessly
                    if (markIfIdentical)
                    {
                        allIdiomMatchingNodes.Add(testedDescendant);
                    }
                }

                if ((!exampleDescendant.IsFragmentRoot && !exampleDescendant.IsTreeLeaf))
                {
                    if (exampleDescendant.Children.Count != testedDescendant.Children.Count)
                    {
                        return false;
                    }
                    else
                    {
                        exampleDescendant.Children.ForEach(c => exampleQueue.Enqueue(c));
                        testedDescendant.Children.ForEach(c => testedQueue.Enqueue(c));
                    }
                }
            }

            if (markIfIdentical)
            {
                foreach (var matchingSubtreeNode in allIdiomMatchingNodes) 
                {
                    matchingSubtreeNode.IdiomMark = ""; // non null value
                }
            }

            return true;
        }

        private List<LabeledNode> FindNodesWithSTInfo(string sTInfo, LabeledTree labeledTree)
        {
            var foundNodes = new List<LabeledNode>();
            DoSearch(sTInfo, labeledTree.Root, foundNodes);

            return foundNodes;
        }

        private void DoSearch(string sTIfo, LabeledNode node, List<LabeledNode> foundNodes)
        {
            if (node.STInfo == sTIfo)
            {
                foundNodes.Add(node);
            }

            foreach (var childNode in node.Children)
            {
                DoSearch(sTIfo, childNode, foundNodes);
            }
        }

        private int CountAllNonBinarySubtreeNodes(LabeledNode rootNode)
        {
            Regex tempNodeRegex = new Regex("^B_[0-9]{4}$");

            Queue<LabeledNode> queue = new Queue<LabeledNode>();
            queue.Enqueue(rootNode);

            var count = 0;
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (!tempNodeRegex.IsMatch(node.STInfo))
                {
                    count++;
                }

                if (node.Children.Count > 0)
                {
                    node.Children.ForEach(ch => queue.Enqueue(ch));
                }
            }

            return count;
        }

        private int CountAllMarkedNonBinarySubtreeNodes(LabeledNode rootNode)
        {
            Regex tempNodeRegex = new Regex("^B_[0-9]{4}$");

            Queue<LabeledNode> queue = new Queue<LabeledNode>();
            queue.Enqueue(rootNode);

            var count = 0;
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                var isNonBinary = !tempNodeRegex.IsMatch(node.STInfo);
                var isMarked = node.IdiomMark != null;
                if (isNonBinary && isMarked)
                {
                    count++;
                }

                if (node.Children.Count > 0)
                {
                    node.Children.ForEach(ch => queue.Enqueue(ch));
                }
            }

            return count;
        }

        private int CalculateIdiomLenght(string idiom)
        {
            var sanitizedIdiom = idiom
                .Trim()
                .Replace("(()", "(%op%)");
            return sanitizedIdiom.Count(f => f == '('); // Relying on the number of ( parenthesis - each node has on in TB notation
        }
    }
}
