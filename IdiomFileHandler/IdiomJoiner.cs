using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS.CSTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdiomFileHandler
{
    internal class IdiomJoiner
    {
        public static void JoinIdioms(string binFilesPath, string idiomMarksCSV)
        {
            ValidateInput(binFilesPath, idiomMarksCSV);
            var fileToTreeList = LoadTrees(binFilesPath);

            var marksToReplace = idiomMarksCSV.Split(',').Select(m => m.Trim()).ToArray();
            var newMark = new Guid().ToString();

            foreach (var tuple in fileToTreeList)
            {
                TraverseToOverwriteIdiomMarks(tuple.Item2.Root, marksToReplace, newMark);
                TraverseToJoinIdioms(tuple.Item2.Root, newMark);
                SaveChangedTree(tuple.Item2, tuple.Item1);
            }
        }

        private static void TraverseToJoinIdioms(LabeledNode node, string newMark)
        {
            if(node == null)
            {
                return;
            }

            if(node.IsTreeLeaf)
            {
                return;
            }

            if (node.IdiomMark == newMark)
            {
                if(node.Parent.IdiomMark == newMark)
                {
                    node.IsFragmentRoot = false;
                }
            }

            var children = node.Children;
            foreach (var child in children)
            {
                TraverseToJoinIdioms(child, newMark);
            }
        }

        private static void SaveChangedTree(CSTree cSTree, FileInfo fileInfo)
        {
            cSTree.Root.Serialize(fileInfo.FullName);
        }

        private static void TraverseToOverwriteIdiomMarks(LabeledNode node, string[] marksToReplace, string newMark)
        {
            var currentMark = node.IdiomMark;
            if(marksToReplace.Contains(currentMark))
            {
                node.IdiomMark = newMark;
            }

            var childNodes = node.Children;
            foreach (var child in childNodes)
            {
                TraverseToOverwriteIdiomMarks(child, marksToReplace, newMark);
            }
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

        private static void ValidateInput(string binFilePath, string idiomMarksCSV)
        {
            if (!Directory.Exists(binFilePath))
            {
                throw new DirectoryNotFoundException(binFilePath);
            }

            string[] strings = idiomMarksCSV.Split(',');
            if (strings.Length < 2)
            {
                throw new Exception("At least two idiom marks should be provided.");
            }
        }
    }
}
