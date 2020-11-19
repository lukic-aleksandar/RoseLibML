using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML
{
    public class LabeledTreeTransformations
    {
        public static void Binarize(LabeledTreeNode parent)
        {
            if (parent.Children.Count > 2)
            {
                var firstChild = parent.Children.FirstOrDefault();
                var restOfChildren = parent.Children.ToList();
                restOfChildren.RemoveAt(0);
                parent.Children.Clear();

                var tempNode = new LabeledTreeNode()
                {
                    LTType = "BinTempNode",
                    UseRoslynMatchToWrite = true // This is on purpose, so it wouldn't get written
                };

                parent.AddChild(firstChild);
                parent.AddChild(tempNode);

                foreach (var child in restOfChildren)
                {
                    tempNode.AddChild(child);
                }

                Binarize(firstChild);
                Binarize(tempNode);
            }
            else
            {
                foreach (var child in parent.Children)
                {
                    Binarize(child);
                }
            }

        }
    }
}
