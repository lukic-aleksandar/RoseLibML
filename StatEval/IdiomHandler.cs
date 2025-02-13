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
        
        public Dictionary<string, List<LabeledNode>> IntermediateIdenticalTrainingSubtreeTestRoots = new Dictionary<string, List<LabeledNode>>(); 
        public Dictionary<string, List<LabeledNode>> FinalIdenticalTrainingSubtreeTestRoots = new Dictionary<string, List<LabeledNode>>();
        public List<string> IdiomsFoundInAsIdenticalSubreesInTest = new List<string>();


        public List<LabeledTree>? TrainingLabeledTrees { get; set; } 
        
        public static IdiomHandler CreateEmptyIdiomHandler()
        {
            return new IdiomHandler(); 
        }

        private IdiomHandler() { }

        #region Calculating statistics

        // "We define idiom set precision as the percentage of
        // the mined idioms found in the test corpus"
        public double CalculatePrecision(List<LabeledTree> testLabeledTrees)
        {
            if (FinalTrainingIdioms == null || FinalTrainingIdioms.Count == 0)
            {
                throw new Exception("Call the function SortOutIdiomsInTrainingSet to populate the structures and be able to compare.");
            }

            IntermediateIdenticalTrainingSubtreeTestRoots.Clear();
            FinalIdenticalTrainingSubtreeTestRoots.Clear();

            foreach (var idiom in FinalTrainingIdioms)
            {
                Parallel.ForEach(testLabeledTrees, (tlt) => FindIdenticalSubtreesPopulateDict(idiom, tlt));

                if(IntermediateIdenticalTrainingSubtreeTestRoots.Count() > 0)
                {
                    IdiomsFoundInAsIdenticalSubreesInTest.Add(idiom);
                    FinalIdenticalTrainingSubtreeTestRoots.Add(idiom, IntermediateIdenticalTrainingSubtreeTestRoots[idiom]);
                    IntermediateIdenticalTrainingSubtreeTestRoots.Clear();
                }
            }

            return IdiomsFoundInAsIdenticalSubreesInTest.Count / (double) FinalTrainingIdioms.Count;
        }

        // "We define idiom coverage as the percent of source code AST
        // nodes that can be matched to the mined idioms"
        public double CalculateCoverage(List<LabeledTree> testLabeledTrees, out int totalMarked)
        {
            // Clear any existing marks
            Parallel.ForEach(testLabeledTrees, (tlt) => ClearAnyNodeMarks(tlt.Root));
            // Find identical subtrees (take a look at the method called above)
            // And mark them! Change the method! :D
            foreach (var idiom in FinalTrainingIdioms)
            {
                Parallel.ForEach(testLabeledTrees, (tlt) => FindIdenticalSubtreesPopulateDict(idiom, tlt, true));
            }

            // count all marked nodes (non binary nodes, I guess)
            // count all nodes (non binary nodes, I guess)
            // divide the two to calculate the percentage
            var totalCount = 0;
            var markedCount = 0;
            foreach(var tlt in testLabeledTrees)
            {
                using(var reader = new StreamReader(tlt.SourceFilePath))
                {
                    var fileContents = reader.ReadToEnd();
                    
                    
                    var totalTreeNodes = CountAllSubtreeNodes(tlt.Root);
                    totalCount += totalTreeNodes;
                    var markedTreeNodes = CountAllMarkedSubtreeNodes(tlt.Root);
                    markedCount += markedTreeNodes;
                }
            }

            totalMarked = markedCount;
            return markedCount / (double)totalCount; 
        }

        public double CalcualteAverageIdiomLength()
        {
            var idiomLengths = new List<int>();
            foreach (var idiom in FinalTrainingIdioms)
            {
                try
                {
                    var idiomLength = CalculateIdiomLength(idiom);
                    idiomLengths.Add(idiomLength);
                }
                catch
                {
                    Console.WriteLine("Was not able to calculate idiom lenght.");
                    Console.WriteLine($"Idiom was {idiom}");
                }
            }

            var averageIdiomLength = idiomLengths.Sum() / (double) FinalTrainingIdioms.Count;
            return averageIdiomLength;
        }

        #endregion

        #region Sorting training idioms
        public void SortOutIdiomsInTrainingSet(int countThreshold, int lengthThreshold)
        {
            if (TrainingLabeledTrees == null)
            {
                throw new Exception("Training labeled trees are not set!");
            }

            foreach (var labeledTree in TrainingLabeledTrees)
            {
                FindTreeIdiomsPopulateIntermediateStructures(labeledTree, labeledTree.Root);
            }

            foreach (var idiom in IntermediateTrainingIdiomRoots.Keys)
            {
                var isCountAboveThreshold = IntermediateTrainingIdiomRoots[idiom].Count >= countThreshold;
                
                var isLengthAboveThreshold = TrainingIdiomLength.ContainsKey(idiom) ? TrainingIdiomLength[idiom] >= lengthThreshold : false;

                if (isCountAboveThreshold && isLengthAboveThreshold)
                {
                    FinalTrainingIdioms.Add(idiom);
                }
            }
        }

        private void FindTreeIdiomsPopulateIntermediateStructures(LabeledTree tree, LabeledNode node)
        {
            if (node.IsTreeRoot() || (node.IsFragmentRoot && !node.IsTreeLeaf))
            {
                var idiomString = node.GetFragmentString();
                NoteIdiomRootAndLength(node, idiomString);
            }

            foreach (var child in node.Children)
            {
                FindTreeIdiomsPopulateIntermediateStructures(tree, child);
            }
        }

        private void NoteIdiomRootAndLength(LabeledNode node, string idiomString)
        {
            bool shouldNote = true;
            if (!IntermediateTrainingIdiomRoots.ContainsKey(idiomString))
            {
                IntermediateTrainingIdiomRoots[idiomString] = new List<LabeledNode>();

                try
                {
                    var idiomLength = CalculateIdiomLength(idiomString);
                    TrainingIdiomLength.Add(idiomString, idiomLength);
                }
                catch
                {
                    Console.WriteLine("Was not able to calculate idiom lenght.");
                    Console.WriteLine($"Idiom was {idiomString}");
                    shouldNote = false;
                }
            }
            if (shouldNote)
            {
                IntermediateTrainingIdiomRoots[idiomString].Add(node);
            }
        }
        #endregion

        #region Finding idioms in test files
        private void ClearAnyNodeMarks(LabeledNode node)
        {
            if (node == null) { return; }
            node.IdiomMark = null;

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    ClearAnyNodeMarks(child);
                }
            }
        }

        private object _addLock = new object();
        private void FindIdenticalSubtreesPopulateDict(string idiom, LabeledTree labeledTree, bool markIfIdentical = false)
        {
            try
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
                            if (!IntermediateIdenticalTrainingSubtreeTestRoots.ContainsKey(idiom))
                            {
                                IntermediateIdenticalTrainingSubtreeTestRoots[idiom] = new List<LabeledNode>();
                            }
                            IntermediateIdenticalTrainingSubtreeTestRoots[idiom].Add(node);
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine($"Skipping idiom, levels were not okay. Idiom ${idiom}");
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
                // Add the tested node in line for marking...
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
                    // If the STInfos are the same, then we can add them, no need to wait to check the children.
                    // If the subtrees are not the same, no permanent marks left, regardlessly.
                    if (markIfIdentical)
                    {
                        allIdiomMatchingNodes.Add(testedDescendant);
                    }
                }

                // If the descendant is not a fragment root nor a tree leaf (it's an internal fragment node)
                if (!exampleDescendant.IsFragmentRoot && !exampleDescendant.IsTreeLeaf)
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
                    matchingSubtreeNode.IdiomMark = ""; //a non-null value
                }
            }

            return true;
        }

        private List<LabeledNode> FindNodesWithSTInfo(string sTInfo, LabeledTree labeledTree)
        {
            var foundNodes = new List<LabeledNode>();
            DoSearchForNodesWithSTInfo(sTInfo, labeledTree.Root, foundNodes);

            return foundNodes;
        }

        private void DoSearchForNodesWithSTInfo(string sTIfo, LabeledNode node, List<LabeledNode> foundNodes)
        {
            if (node.STInfo == sTIfo)
            {
                foundNodes.Add(node);
            }

            foreach (var childNode in node.Children)
            {
                DoSearchForNodesWithSTInfo(sTIfo, childNode, foundNodes);
            }
        }
        #endregion

        #region Calcuating statistics - util methods
        private int CountAllSubtreeNodes(LabeledNode rootNode)
        {
            Queue<LabeledNode> queue = new Queue<LabeledNode>();
            queue.Enqueue(rootNode);

            var count = 0;
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
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

        private int CountAllMarkedSubtreeNodes(LabeledNode rootNode)
        {
            Queue<LabeledNode> queue = new Queue<LabeledNode>();
            queue.Enqueue(rootNode);

            var count = 0;
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                var isMarked = node.IdiomMark != null;
                if(isMarked)
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

        private int CalculateIdiomNodesCount(string idiom)
        {
            var sanitizedIdiom = idiom
                .Trim()
                .Replace("(()", "(%op%)");

            return sanitizedIdiom.Count(f => f == '('); // Relying on the number of ( parenthesis - each node has on in TB notation
        }


        public int CalculateIdiomLength(string idiom)
        {
            var rootNode = DeserializeTBNIdiom(idiom);

            var queue = new Queue<CSNode>();
            var leafs = new List<CSNode>();

            queue.Enqueue(rootNode);
            while (queue.Count > 0)
            {
                var currentNode = queue.Dequeue();
                if (currentNode.Children.Count > 0)
                {
                    foreach (var child in currentNode.Children)
                    {
                        queue.Enqueue(child as CSNode);
                    }
                }
                else
                {
                    leafs.Add(currentNode);
                }
            }

            return leafs.Count;
        }

        private CSNode DeserializeTBNIdiom(string idiom)
        {
            var sanitizedIdiom = idiom
                            .Trim()
                            .Replace("(()", "(%op%)")
                            .Replace("())", "(%cp%)");

            var level = 0;
            var currentWord = "";

            var idiomReadyForDeserialization = sanitizedIdiom
                .Substring(1, sanitizedIdiom.Length - 2)
                .Trim();

            var currentNode = new CSNode();
            var rootNode = currentNode;

            foreach (var ch in idiomReadyForDeserialization)
            {
                if (ch == '(')
                {
                    level++;
                    if (currentNode.STInfo == null || currentNode.STInfo.Length == 0)
                    {
                        currentNode.STInfo = currentWord.Trim();
                        if (currentNode.STInfo == "%op%") currentNode.STInfo = "(";
                        if (currentNode.STInfo == "%cp%") currentNode.STInfo = ")";
                    }
                    currentWord = "";

                    var newChildNode = new CSNode();

                    newChildNode.Parent = currentNode;
                    currentNode.Children.Add(newChildNode);

                    currentNode = newChildNode;
                    continue;
                }
                if (ch == ')')
                {
                    level--;

                    if (currentNode.STInfo == null || currentNode.STInfo.Length == 0)
                    {
                        currentNode.STInfo = currentWord.Trim();
                        if (currentNode.STInfo == "%op%") currentNode.STInfo = "(";
                        if (currentNode.STInfo == "%cp%") currentNode.STInfo = ")";
                    }
                    currentWord = "";

                    currentNode = (CSNode)currentNode.Parent;
                    continue;
                }

                currentWord += ch;
            }

            return rootNode;
        }
        #endregion
    }
}
