using RoseLibML.Core.LabeledTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML
{
    public class LabeledTreeTransformations
    {

        public static void Binarize(LabeledNode parent, NodeCreator nodeCreator)
        {
            if(parent.Children.Count > 2) // Zašto gledam broj svih, a ne samo "neterminala"?
            {
                var groups = FindSuccessiveNonLeavesGroups(parent.Children);
                groups.Reverse();

                foreach (var group in groups)
                {
                    var indexOfFirst = parent.Children.IndexOf(group.First());
                    parent.Children.RemoveRange(indexOfFirst, group.Count);

                    var tempNode = nodeCreator.CreateTempNode();
                    tempNode.Parent = parent;
                    if (parent.STInfo.StartsWith("B_"))
                    {
                        tempNode.STInfo = parent.STInfo;
                    }
                    else
                    {
                        tempNode.STInfo = "B_" + parent.STInfo;
                    }

                    var firstGroupChild = group.FirstOrDefault();
                    var restOfGroupChildren = group.ToList();
                    restOfGroupChildren.RemoveAt(0);

                    foreach (var child in restOfGroupChildren)
                    {
                        tempNode.AddChild(child);
                    }

                    parent.Children.Insert(indexOfFirst, firstGroupChild);
                    parent.Children.Insert(indexOfFirst + 1, tempNode);
                }
            }
            
            foreach (var child in parent.Children)
            {
                Binarize(child, nodeCreator);
            }
            
        }

        private static List<List<LabeledNode>> FindSuccessiveNonLeavesGroups(List<LabeledNode> children)
        {
            var retVal = new List<List<LabeledNode>>();

            var successive = new List<LabeledNode>();
            for (int i = 0; i < children.Count; i++) 
            {
                if (!children[i].IsTreeLeaf)
                {
                    successive.Add(children[i]);
                }
                else
                {
                    if(successive.Count > 2)
                    {
                        var temp = new List<LabeledNode>(successive);
                        retVal.Add(temp);
                    }

                    successive.Clear();
                }
            }
            if(successive.Count > 2)
            {
                retVal.Add(successive);
            }

            return retVal;
        }
    }
}
