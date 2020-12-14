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

        public static (LabeledNode full, LabeledNode part1, LabeledNode part2) GetFragments(this LabeledNode labeledNode)
        {
            var oldIsFragmentRoot = labeledNode.IsFragmentRoot;
            labeledNode.IsFragmentRoot = false;
            var fragmentRoot = labeledNode.FindFragmentRoot();
            var full = fragmentRoot.DuplicateFragment();
            labeledNode.IsFragmentRoot = true;
            var part1 = fragmentRoot.DuplicateFragment();
            var part2 = labeledNode.DuplicateFragment();
            labeledNode.IsFragmentRoot = oldIsFragmentRoot;

            return (full: full, part1: part1, part2: part2);
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
