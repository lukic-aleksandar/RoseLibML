namespace StatEval
{
    internal class IdiomHandler
    {

        public Dictionary<string, List<LabeledNode>> IdiomRoots = new Dictionary<string, List<LabeledNode>>();
        public Dictionary<string, HashSet<string>> IdiomsInFiles = new Dictionary<string, HashSet<string>>();
        public Dictionary<string, List<LabeledNode>> IdenticalSubtreeRoots = new Dictionary<string, List<LabeledNode>>();


        public List<LabeledTree> labeledTrees;
        public IdiomHandler(List<LabeledTree> labeledTrees)
        {
            this.labeledTrees = labeledTrees;
        }

        public Dictionary<string, List<LabeledNode>> SortOutIdiomsInTrainingSet()
        {
            foreach (var labeledTree in labeledTrees)
            {
                FindTreeIdioms(labeledTree, labeledTree.Root);
            }

            return IdiomRoots;
        }

        private void FindTreeIdioms(LabeledTree tree, LabeledNode node)
        {
            if (node.IsTreeRoot() || (node.IsFragmentRoot && !node.IsTreeLeaf))
            {
                var idiomString = node.GetFragmentString();
                AddToIdiomRootsDict(node, idiomString);
                AddToIdiomFilesDict(tree, idiomString);
            }

            foreach (var child in node.Children)
            {
                FindTreeIdioms(tree, child);
            }
        }

        private void AddToIdiomFilesDict(LabeledTree tree, string idiomString)
        {
            if (!IdiomsInFiles.ContainsKey(idiomString))
            {
                IdiomsInFiles[idiomString] = new HashSet<string>();
            }
            IdiomsInFiles[idiomString].Add(tree.FileName);
        }

        private void AddToIdiomRootsDict(LabeledNode node, string idiomString)
        {
            if (!IdiomRoots.ContainsKey(idiomString))
            {
                IdiomRoots[idiomString] = new List<LabeledNode>();
            }
            IdiomRoots[idiomString].Add(node);
        }


        private object _addLock = new object();
        private void FindIdenticalSubtrees(string idiom, LabeledTree labeledTree)
        {
            var exampleSubtreeRoot = IdiomRoots[idiom].FirstOrDefault();
            if (exampleSubtreeRoot == null)
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
            if (exampleRootNode == testedRootNode)
            {
                return true;
            }

            if (exampleRootNode.Children.Count != testedRootNode.Children.Count)
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
    }
}
