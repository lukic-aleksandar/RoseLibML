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
            if (parent.Children.Count > 2)
            {
                var firstChild = parent.Children.FirstOrDefault();
                var restOfChildren = parent.Children.ToList();
                restOfChildren.RemoveAt(0);
                parent.Children.Clear();

                var tempNode = nodeCreator.CreateTempNode();
                tempNode.STInfo = "BinTempNode";

                parent.AddChild(firstChild);
                parent.AddChild(tempNode);

                foreach (var child in restOfChildren)
                {
                    tempNode.AddChild(child);
                }

                Binarize(firstChild, nodeCreator);
                Binarize(tempNode, nodeCreator);
            }
            else
            {
                foreach (var child in parent.Children)
                {
                    Binarize(child, nodeCreator);
                }
            }

        }
    }
}
