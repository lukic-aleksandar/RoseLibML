using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoseLibML
{
    [Serializable]
    public class LabeledTreeNode
    {
        public bool CanHaveType { get; set; } = true;
        public bool IsFragmentRoot { get; set; }
        public string ASTNodeType { get; set; }
        public LabeledTreeNode Parent { get; set; }
        public List<LabeledTreeNode> Children { get; set; } = new List<LabeledTreeNode>();

        [NonSerialized]
        private (int typeCode, short iteration) lastModified = (typeCode: -1, iteration: -1);
        public (int typeCode, short iteration) LastModified { get => lastModified; set { lastModified = value; } }

        [NonSerialized]
        private LabeledTreeNodeType type;
        public LabeledTreeNodeType Type { get => type; set { type = value; } }

        public void Serialize(string filePath)
        {
            BinaryFormatter b = new BinaryFormatter();
            var fileStream = File.Create(filePath);
            b.Serialize(fileStream, this);
            fileStream.Close();
        }

        public static LabeledTreeNode Deserialize(string filePath)
        {
            try
            {
                BinaryFormatter b = new BinaryFormatter();
                var fileStream = File.OpenRead(filePath);
                var treeNode = (LabeledTreeNode)b.Deserialize(fileStream);
                fileStream.Close();
                return treeNode;
            }
            catch
            {

            }


            return null;
        }

        public void AddChild(LabeledTreeNode child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public static LabeledTreeNodeType GetType(LabeledTreeNode labeledNode)
        {
            var oldIsFragmentRoot = labeledNode.IsFragmentRoot;
            labeledNode.IsFragmentRoot = false;
            var fragmentRoot = FindFragmentRoot(labeledNode);
            var full = GetFragmentString(fragmentRoot);
            labeledNode.IsFragmentRoot = true;
            var part1 = GetFragmentString(fragmentRoot);
            var part2 = GetFragmentString(labeledNode);
            labeledNode.IsFragmentRoot = oldIsFragmentRoot;

            return new LabeledTreeNodeType()
            {
                FullFragment = full,
                Part1Fragment = part1,
                Part2Fragment = part2
            };
        }

        public static (LabeledTreeNode full, LabeledTreeNode part1, LabeledTreeNode part2) GetFragments(LabeledTreeNode labeledNode)
        {
            var oldIsFragmentRoot = labeledNode.IsFragmentRoot;
            labeledNode.IsFragmentRoot = false;
            var fragmentRoot = FindFragmentRoot(labeledNode);
            var full = GetFragment(fragmentRoot);
            labeledNode.IsFragmentRoot = true;
            var part1 = GetFragment(fragmentRoot);
            var part2 = GetFragment(labeledNode);
            labeledNode.IsFragmentRoot = oldIsFragmentRoot;

            return (full: full, part1: part1, part2: part2);
        }

        public static LabeledTreeNode GetFragment(LabeledTreeNode labeledNode)
        {
            var node = new LabeledTreeNode();
            node.CopySimpleProperties(labeledNode);

            var parent = new LabeledTreeNode();
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
                    var childCopy = new LabeledTreeNode();
                    childCopy.CopySimpleProperties(child);
                    node.AddChild(childCopy);
                }
            }

            return node;
        }

        public static string GetFragmentString(LabeledTreeNode labeledNode)
        {
            var fragmentString = labeledNode.ASTNodeType;

            if(labeledNode.Children.Count > 0)
            {
                var childrenString = "";

                foreach (var child in labeledNode.Children)
                {
                    if (child.IsFragmentRoot || !child.CanHaveType)
                    {
                        childrenString += $" {child.ASTNodeType} ";
                    }
                    else
                    {
                        childrenString += GetFragmentString(child);
                    }
                }

                fragmentString += $" ( {childrenString} ) ";
            }

            return fragmentString;
        }

        public void CopySimpleProperties(LabeledTreeNode labeledNode)
        {
            if(labeledNode != null)
            {
                ASTNodeType = labeledNode.ASTNodeType;
                CanHaveType = labeledNode.CanHaveType;
                IsFragmentRoot = labeledNode.IsFragmentRoot;
            }
        }

        public override string ToString()
        {
            return ASTNodeType;
        }

        public static LabeledTreeNode FindFullFragmentRoot(LabeledTreeNode labeledNode)
        {
            if(labeledNode.Parent != null)
            {
                return FindFragmentRoot(labeledNode.Parent);
            }
            else
            {
                return labeledNode;
            }
        }

        public static LabeledTreeNode FindFragmentRoot(LabeledTreeNode labeledNode)
        {
            var fragmentRoot = labeledNode;

            if(fragmentRoot.ASTNodeType == "CompilationUnit")
            {
                return fragmentRoot;
            }

            while (!fragmentRoot.IsFragmentRoot)
            {
                fragmentRoot = fragmentRoot.Parent;
            }

            return fragmentRoot;
        }
    }
}
