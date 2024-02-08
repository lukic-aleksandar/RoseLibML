using RoseLibML.Core;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS.CSTrees;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdiomFileGrouper
{
    public class IdiomFileGrouper
    {
        public static List<FileInfo> GroupFilesBasedOnIdiomMark(string binFilesPath, string idiomMark)
        {
            List<FileInfo> matchingFilesGroup = new List<FileInfo>();

            ValidateInput(binFilesPath, idiomMark);

            var fileToTreeList = LoadTrees(binFilesPath);

            if (fileToTreeList.Count == 0)
            {
                return matchingFilesGroup;
            }

            
            LabeledTree? firstFoundTree = null;


            LabeledNode? idiomRootNode = null;
            foreach (var tuple in fileToTreeList)
            {
                idiomRootNode = TraverseToFindNodeWithMark(tuple.Item2.Root, idiomMark);

                if (idiomRootNode != null)
                {
                    matchingFilesGroup.Add(tuple.Item1);
                    firstFoundTree = tuple.Item2;
                    break;
                }
            }

            if (idiomRootNode == null)
            {
                return matchingFilesGroup;
            }

            foreach (var tuple in fileToTreeList)
            {
                if (tuple.Item2 == firstFoundTree)
                {
                    continue;
                }

                if(HasSameSubtree(idiomRootNode, tuple.Item2))
                {
                    matchingFilesGroup.Add(tuple.Item1);
                }
            }

            return matchingFilesGroup;
        }

        private static bool HasSameSubtree(LabeledNode idiomRootNode, LabeledTree tree)
        {
            var idiomNodesList = CreateReferenceIdiomList(idiomRootNode);
            var sameSTInfoNodes = FindNodesWithSameSTInfo(idiomRootNode.STInfo, tree);

            foreach (var subtreeRoot in sameSTInfoNodes)
            {
                var areSame = CompareSubtreeToReference(idiomNodesList, subtreeRoot);
                if (areSame)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CompareSubtreeToReference(List<LabeledNode> referenceIdiomNodesList, LabeledNode sameSTInfoNode)
        {
            var testedSubtreeList = new List<LabeledNode>() { sameSTInfoNode };
            for (int i = 0; i < referenceIdiomNodesList.Count; i++)
            {
                var referenceNode = referenceIdiomNodesList[i];
                var testedNode = testedSubtreeList[i];

                // Are node kinds the same
                if (!referenceNode.STInfo.Equals(testedNode.STInfo))
                {
                    // They are not, the idiom similarity ends here
                    return false;
                }

                // Does the branch comparison stop here?
                if (referenceNode.Children == null || referenceNode.Children.Count == 0)
                {   
                    // It does, just continue...
                    continue;
                }
                else
                {
                    // It might not, need to check further...

                    // Test if any children of the reference node
                    // have been added to the reference list for testing
                    var firstChild = referenceNode.Children[0];
                    if (referenceIdiomNodesList.Contains(firstChild))
                    {
                        // It does

                        // Unable to add children of the tested node to the list
                        if (testedNode.Children == null || testedNode.Children.Count == 0)
                        {
                            return false;
                        }

                        // Is the number of children the same?
                        if (testedNode.Children.Count != referenceNode.Children.Count)
                        {
                            // Not the same number of children, meaning that idiom similarity stops here
                            return false;
                        }

                        testedSubtreeList.AddRange(testedNode.Children);
                    }
                }
            }

            // Passed all the checks? Great, these two endeed are the same!
            return true;
        }

        private static List<LabeledNode> FindNodesWithSameSTInfo(string sTInfo, LabeledTree tree)
        {
            var sameSTInfoNodes = new List<LabeledNode>();
            var rootNode = tree.Root;

            TraverseToFindNodesWithSameSTInfo(sTInfo, rootNode, sameSTInfoNodes);

            return sameSTInfoNodes;
        }

        private static void TraverseToFindNodesWithSameSTInfo(string sTInfo, LabeledNode node, List<LabeledNode> sameSTInfoNodes)
        {
            if (node.STInfo == sTInfo)
            {
                sameSTInfoNodes.Add(node);
            }

            if (node.Children == null || node.Children.Count > 0)
            {
                return;
            }

            foreach (var child in node.Children)
            {
                TraverseToFindNodesWithSameSTInfo(sTInfo, child, sameSTInfoNodes);
            }
        }

        private static List<LabeledNode> CreateReferenceIdiomList(LabeledNode idiomRootNode)
        {
            var listToProcess = new List<LabeledNode>() { idiomRootNode };

            var children = idiomRootNode.Children;
            if (children == null || children.Count == 0)
            {
                return listToProcess;
            }

            var resultingList = new List<LabeledNode>();
            while (listToProcess.Count > 0)
            {
                var firstElement = listToProcess[0];

                resultingList.Add(firstElement);
                listToProcess.RemoveAt(0);

                if (!(firstElement.IsFragmentRoot || firstElement.IsTreeLeaf))
                {
                    if (firstElement.Children != null && firstElement.Children.Count > 0)
                    {
                        listToProcess.AddRange(firstElement.Children);
                    }
                }
            }

            return resultingList;
        }

        private static LabeledNode? TraverseToFindNodeWithMark(LabeledNode node, string mark)
        {
            if (node.IdiomMark == mark)
            {
                return node;
            }

            foreach (var child in node.Children)
            {
                var retVal = TraverseToFindNodeWithMark(child, mark);
                if (retVal != null)
                {
                    return retVal;
                }
            }

            return null;
        }

        private static List<Tuple<FileInfo, CSTree>> LoadTrees(string binFilesPath)
        {
            var binDirectoryInfo = new DirectoryInfo(binFilesPath);
            var binFileInfos = binDirectoryInfo.GetFiles("*.bin");

            var fileToTreeList = new List<Tuple<FileInfo, CSTree>>();
            foreach (var binFileInfo in binFileInfos)
            {
                var tree = new CSTree();
                tree.Root = CSNode.Deserialize(binFileInfo.FullName);

                fileToTreeList
                    .Add(new Tuple<FileInfo, CSTree>(binFileInfo, tree));
            }

            return fileToTreeList;
        }


        private static void ValidateInput(string binFilePath, string idiomMark)
        {
            if (!Directory.Exists(binFilePath))
            {
                throw new DirectoryNotFoundException(binFilePath);
            }

            if (string.IsNullOrEmpty(idiomMark))
            {
                throw new ArgumentException($"Value provided for idiom mark not valid");
            }
        }
    }
}
