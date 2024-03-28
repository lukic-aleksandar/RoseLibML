using IdiomHtmlVisualizer.Model;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS;
using RoseLibML.CS.CSTrees;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IdiomHtmlVisualizer
{
    public class IdiomHandler
    {

        public Dictionary<string, List<LabeledNode>> IdiomRoots = new Dictionary<string, List<LabeledNode>>();
        public Dictionary<string, HashSet<string>> IdiomsInFiles = new Dictionary<string, HashSet<string>>();
        public Dictionary<string, List<LabeledNode>> IdenticalSubtreeRoots = new Dictionary<string, List<LabeledNode>>();
        public Dictionary<string, string> MarksIdiomsMap = new Dictionary<string, string>();
        public Dictionary<string, string> IdiomsMarksMap = new Dictionary<string, string>();
        public Dictionary<string, string> MarksIdiomCodeMap = new Dictionary<string, string>();


        public List<LabeledTree> labeledTrees;
        public IdiomHandler(List<LabeledTree> labeledTrees)
        {
            this.labeledTrees = labeledTrees;
        }

        public Dictionary<string, List<LabeledNode>> SortOutIdioms()
        {
            foreach (var labeledTree in labeledTrees)
            {
                FindTreeIdioms(labeledTree, labeledTree.Root);
            }

            return IdiomRoots;
        }

        public Dictionary<string, List<LabeledNode>> SortOutSubtreesBasedOnIdioms(int idiomLengthThreshold)
        {
            if (!IdiomRoots.Keys.Any())
            {
                SortOutIdioms();
            }

            foreach (var idiom in IdiomRoots.Keys)
            {
                if (IsLengthBelowThreshold(idiom, idiomLengthThreshold))
                {
                    continue;
                }

                IdenticalSubtreeRoots.Add(idiom, new List<LabeledNode>());

                Parallel.ForEach(labeledTrees, t => FindIdenticalSubtrees(idiom, t));
                //foreach(var labeledTree in labeledTrees)
                //{
                //    FindIdenticalSubtrees(idiom, labeledTree);
                //}
            }

            return IdenticalSubtreeRoots;
        }

        // It should count the number of nodes in the idiom
        // Number of openned parenthesis in the treebank notation idiom representation approximates this
        // Special cases like strings containing ( are not taken care of
        private bool IsLengthBelowThreshold(string idiom, int lengthThreshold)
        {
            var approximateNoOfNodes = idiom.Count(c => c == '(');
            Regex doubleOpenParenthesisRE = new Regex("\\(\\(");
            var doubleParenthesis = doubleOpenParenthesisRE.Matches(idiom);

            var betterApproximation = approximateNoOfNodes - doubleParenthesis.Count;
            if (betterApproximation < lengthThreshold)
            {
                return true;
            }

            return false;
        }

        private void FindTreeIdioms(LabeledTree tree, LabeledNode node)
        {
            if (node.IsTreeRoot() || (node.IsFragmentRoot && !node.IsTreeLeaf))
            {
                var idiomString = node.GetFragmentString();
                if (!IdiomRoots.ContainsKey(idiomString))
                {
                    IdiomRoots[idiomString] = new List<LabeledNode>();
                }
                IdiomRoots[idiomString].Add(node);

                if (!IdiomsInFiles.ContainsKey(idiomString))
                {
                    IdiomsInFiles[idiomString] = new HashSet<string>();
                }
                IdiomsInFiles[idiomString].Add(tree.FileName);
            }

            foreach (var child in node.Children)
            {
                FindTreeIdioms(tree, child);
            }
        }

        public void MarkAllIdiomRootNodes()
        {
            foreach (var idiom in IdiomRoots.Keys)
            {
                var list = IdiomRoots[idiom];
                var newIdiomMark = Guid.NewGuid().ToString();
                MarksIdiomsMap.Add(newIdiomMark, idiom);
                IdiomsMarksMap.Add(idiom, newIdiomMark);
                foreach (var rootNode in list)
                {
                    rootNode.IdiomMark = newIdiomMark;
                }
            }
        }

        public void GenerateCodeFragmentsForAllIdioms()
        {

            foreach (var idiom in IdiomRoots.Keys)
            {
                ToCSWriter writer = new ToCSWriter();
                writer.InitializeForSingleIdiomUsage(labeledTrees.ToArray());

                var idiomRoot = IdiomRoots[idiom].First();
                var jsonWithCodeFragment = writer.SingleFragmentToString((CSNode)idiomRoot);
                MarksIdiomCodeMap.Add(idiomRoot.IdiomMark, jsonWithCodeFragment);
            }
        }

        public void MarkIdiomNodes(CSNode node)
        {
            if (!(node.IsTreeRoot() || (node.IsFragmentRoot && !node.IsTreeLeaf)))
            {
                node.IdiomMark = node.Parent.IdiomMark;
            }

            foreach (var child in node.Children)
            {
                MarkIdiomNodes((child as CSNode)!);
            }
        }

        private object _addLock = new object();
        private void FindIdenticalSubtrees(string idiom, LabeledTree labeledTree)
        {
            var exampleSubtreeRoot = IdiomRoots[idiom].FirstOrDefault();
            if(exampleSubtreeRoot == null)
            {
                throw new ArgumentException("Provided with an idiom not present in idiom roots dictionary");
            }

            var foundNodes = FindNodesWithSTInfo(exampleSubtreeRoot.STInfo, labeledTree);
            foreach (var node in foundNodes)
            {
                if (RootIdenticalSubtrees(exampleSubtreeRoot, node))
                {
                    lock (_addLock)
                    {
                        IdenticalSubtreeRoots[idiom].Add(node);
                    }
                }
            }
        }

        private bool RootIdenticalSubtrees(LabeledNode exampleRootNode, LabeledNode testedRootNode)
        {
            if(exampleRootNode == testedRootNode)
            {
                return true;
            }

            if(exampleRootNode.Children.Count != testedRootNode.Children.Count) 
            {
                return false;
            }

            Queue<LabeledNode> exampleQueue = new Queue<LabeledNode>(exampleRootNode.Children);
            Queue<LabeledNode> testedQueue = new Queue<LabeledNode>(testedRootNode.Children);


            while (exampleQueue.Count > 0)
            {
                var exampleDescendant = exampleQueue.Dequeue();
                var testedDescendant = testedQueue.Dequeue();

                if (exampleDescendant.STInfo != testedDescendant.STInfo)
                {
                    return false;
                }

                if (!exampleDescendant.IsFragmentRoot && !exampleDescendant.IsTreeLeaf)
                {
                    if(exampleDescendant.Children.Count != testedDescendant.Children.Count)
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
            if(node.STInfo == sTIfo)
            {
                foundNodes.Add(node);
            }

            foreach(var childNode in node.Children) 
            {
                DoSearch(sTIfo, childNode, foundNodes);
            }
        }
    }
}
