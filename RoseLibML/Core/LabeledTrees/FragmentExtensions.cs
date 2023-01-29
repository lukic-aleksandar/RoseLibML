using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.Core.LabeledTrees
{
    public static class FragmentExtensions
    {
        // Finds fragment root, which could include passed labeled node.
        // So, the root is note necessarily "above" the passed node
        public static LabeledNode FindFragmentRoot(this LabeledNode labeledNode)
        {
            var fragmentRoot = labeledNode;

            while (!fragmentRoot.IsFragmentRoot)
            {
                if (fragmentRoot.IsTreeRoot())
                {
                    return fragmentRoot;
                }

                fragmentRoot = fragmentRoot.Parent;
            }

            return fragmentRoot;
        }

        // Finds nearest fragment root above, if such could exist.
        // Else, returns given node (assumption is that given node is root node, then)
        public static LabeledNode FindFullFragmentRoot(this LabeledNode labeledNode)
        {
            if (labeledNode.Parent != null)
            {
                return labeledNode.Parent.FindFragmentRoot();
            }
            else
            {
                return labeledNode;
            }
        }

        /// <summary>
        /// It is guaranteed that the first node in the array will be the full fragment root
        /// </summary>
        /// <param name="labeledNode"></param>
        /// <returns>List of all fragment nodes - flattened</returns>
        /// <exception cref="Exception">Just added a possible check for a situation that would be interesting to me
        /// - that someone called this method on a tree root</exception>
        public static List<LabeledNode> GetAllFullFragmentNodes(this LabeledNode labeledNode)
        {
            List<LabeledNode> fragmentNodes = new List<LabeledNode>();
            var fullFragmentRoot = labeledNode.FindFullFragmentRoot();

            if(fullFragmentRoot == null || fullFragmentRoot == labeledNode)
            {
                throw new Exception("It did happen! :)");
            }

            fragmentNodes.Add(fullFragmentRoot);

            var fragmentDescendants = GetAllFragmentNodes(fullFragmentRoot, labeledNode); // Labeled node is the pivot
            fragmentNodes.AddRange(fragmentDescendants);

            return fragmentNodes;
        }

        private static List<LabeledNode> GetAllFragmentNodes(LabeledNode root, LabeledNode pivot)
        {
            var listOfNodes = new List<LabeledNode>();
            listOfNodes.AddRange(root.Children);

            foreach (var child in root.Children)
            {
                if (!child.IsFragmentRoot || child == pivot)
                {
                    var childsDescendants = GetAllFragmentNodes(child, pivot);
                    if (childsDescendants != null && childsDescendants.Count > 0)
                    {
                        listOfNodes.AddRange(childsDescendants);
                    }
                }
            }

            return listOfNodes;
        }



        public static List<LabeledNode> GetAllFullFragmentLeaves(this LabeledNode labeledNode)
        {
            List<LabeledNode> fragmentNodes = new List<LabeledNode>();
            var fullFragmentRoot = labeledNode.FindFullFragmentRoot();

            if (fullFragmentRoot == null || fullFragmentRoot == labeledNode)
            {
                throw new Exception("It did happen! :)");
            }

            var fragmentDescendants = GetAllFragmentLeaves(fullFragmentRoot, labeledNode); // Labeled node is the pivot
            fragmentNodes.AddRange(fragmentDescendants);

            return fragmentNodes;
        }

        private static List<LabeledNode> GetAllFragmentLeaves(LabeledNode root, LabeledNode pivot)
        {
            var leaves = new List<LabeledNode>();

            foreach (var child in root.Children)
            {
                if ((child.IsFragmentRoot || child.IsTreeLeaf) && child != pivot)
                {
                    leaves.Add(child);
                }
                else
                {
                    var descendantLeaves = GetAllFragmentLeaves(child, pivot);
                    if (descendantLeaves != null && descendantLeaves.Count > 0)
                    {
                        leaves.AddRange(descendantLeaves);
                    }
                }
            }

            return leaves;
        }




        public static string GetFragmentString(this LabeledNode labeledNode)
        {
            var stringWriter = new StringWriter();
            WriteFragmentString(labeledNode, stringWriter);

            var retVal = stringWriter.ToString();
            stringWriter.Dispose();
            return retVal;
                       
        }

        private static void WriteFragmentString(LabeledNode labeledNode, StringWriter stringWriter)
        {
            // var fragmentString = $"({labeledNode.STInfo})";
            stringWriter.Write($"({labeledNode.STInfo} ");

            if (labeledNode.Children.Count > 0)
            {
                // var childrenString = "";

                foreach (var child in labeledNode.Children)
                {
                    if ((child.IsFragmentRoot) || !child.CanHaveType)
                    {
                        stringWriter.Write($"({child.STInfo}) ");
                    }
                    else
                    {
                        //childrenString += GetFragmentString(child);
                        WriteFragmentString(child, stringWriter);
                    }
                }

                // fragmentString = $"({labeledNode.STInfo} {childrenString} ) ";
            }

            stringWriter.Write(" ) ");
        }

        public static (LabeledNode full, LabeledNode part1, LabeledNode part2) GetRootNodesForTypeFragments(this LabeledNode labeledNode)
        {
            var oldIsFragmentRoot = labeledNode.IsFragmentRoot;
            labeledNode.IsFragmentRoot = false;
            var fragmentRoot = labeledNode.FindFragmentRoot();
            var full = fragmentRoot.DuplicateFragment();
            labeledNode.IsFragmentRoot = true;
            var part1 = fragmentRoot.DuplicateFragment();
            var part2 = labeledNode.DuplicateFragment();
            labeledNode.IsFragmentRoot = oldIsFragmentRoot;

            return (full, part1, part2);
        }

        // Kreira duplikat fragmenta! Dakle, svih čvorova u fragmentu!
        public static LabeledNode DuplicateFragment(this LabeledNode labeledNode)
        {
            var nodeDuplicate = labeledNode.CreateSimpleDuplicate();
            
            if(labeledNode.Parent != null)
            {
                var parentDuplicate = labeledNode.Parent.CreateSimpleDuplicate();
                nodeDuplicate.Parent = parentDuplicate;
            }

            foreach (var child in labeledNode.Children)
            {
                if (!child.IsFragmentRoot)
                {
                    nodeDuplicate.AddChild(DuplicateFragment(child));
                }
                else
                {
                    var childDuplicate = child.CreateSimpleDuplicate();
                    nodeDuplicate.AddChild(childDuplicate);
                }
            }

            return nodeDuplicate;
        }
    }
}
