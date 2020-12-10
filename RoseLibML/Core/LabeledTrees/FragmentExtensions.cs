using System;
using System.Collections.Generic;
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
                if (fragmentRoot.STInfo == "8840") // 8840 == CompilationUnit
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
            var fragmentString = $"({labeledNode.STInfo})";

            if (labeledNode.Children.Count > 0)
            {
                var childrenString = "";

                foreach (var child in labeledNode.Children)
                {
                    if ((child.IsFragmentRoot) || !child.CanHaveType)
                    {
                        childrenString += $"({child.STInfo}) ";
                    }
                }

                fragmentString = $"({labeledNode.STInfo} {childrenString} ) ";
            }

            return fragmentString;
        }

        public static (LabeledNode full, LabeledNode part1, LabeledNode part2) GetFragments(this LabeledNode labeledNode)
        {
            var oldIsFragmentRoot = labeledNode.IsFragmentRoot;
            labeledNode.IsFragmentRoot = false;
            var fragmentRoot = labeledNode.FindFragmentRoot();
            var full = fragmentRoot.GetFragment();
            labeledNode.IsFragmentRoot = true;
            var part1 = fragmentRoot.GetFragment();
            var part2 = labeledNode.GetFragment();
            labeledNode.IsFragmentRoot = oldIsFragmentRoot;

            return (full: full, part1: part1, part2: part2);
        }

        // Kreira kopiju fragmenta! Dakle, svih čvorova u fragmentu!
        public static LabeledNode GetFragment(this LabeledNode labeledNode)
        {
            var node = new LabeledNode();
            node.CopySimpleProperties(labeledNode);

            var parent = new LabeledNode();
            parent.CopySimpleProperties(node.Parent);

            node.Parent = parent;

            foreach (var child in labeledNode.Children)
            {
                if (!child.IsFragmentRoot)
                {
                    node.AddChild(GetFragment(child));
                }
                else
                {
                    var childCopy = new LabeledNode();
                    childCopy.CopySimpleProperties(child);
                    node.AddChild(childCopy);
                }
            }

            return node;
        }
    }
}
