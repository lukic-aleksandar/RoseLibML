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
using System.Diagnostics;

namespace RoseLibML
{
    [Serializable]
    public class LabeledTreeNode
    {
        public bool CanHaveType { get; set; } = true;

        private bool isFragmentRoot;
        public bool IsFragmentRoot
        {
            get
            {
                if (!CanHaveType)
                {
                    return false;
                }
                return isFragmentRoot;
            }
            set => isFragmentRoot = value;
        }
        public string LTType { get; set; } // Labeled Tree Type - serves to hold Roslyn Syntax type, some concrete value, or custom type.
        public bool UseRoslynMatchToWrite { get; set; }
        public int RoslynSpanStart { get; set; }
        public int RoslynSpanEnd { get; set; }
        public LabeledTreeNode Parent { get; set; }
        public List<LabeledTreeNode> Children { get; set; } = new List<LabeledTreeNode>();

        [NonSerialized]
        private (string typeCode, short iteration) lastModified = (typeCode: "", iteration: -1);
        public (string typeCode, short iteration) LastModified { get => lastModified; set { lastModified = value; } }

        [NonSerialized]
        private LabeledTreeNodeType type;
        public LabeledTreeNodeType Type
        {
            get => type;
            set
            {
                type = value;

                // var trueType = GetType(this);
                // if (!trueType.Equals(type))
                // {
                //     throw new Exception("Given type is wrong!");
                // }

                // typeHistory.Add(value);
                // var stackTrace = new StackTrace();
                // var callerName = stackTrace.GetFrame(1).GetMethod().Name;
                // setTypeCallHistory.Add(callerName);
            }
        }

        // private List<LabeledTreeNodeType> typeHistory = new List<LabeledTreeNodeType>();
        // private List<string> setTypeCallHistory = new List<string>();

        public bool IsExistingRoslynNode
        {
            get { return ushort.TryParse(this.LTType, out ushort result); }
        }

        public bool CouldBeWritten
        {
            get
            {
                // A small hack!. 
                // To avoid adding new fields, I used this state to
                // denote that it shouldn't even be written.
                if (!IsExistingRoslynNode && UseRoslynMatchToWrite) 
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

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

        public static string GetFragmentString(LabeledTreeNode labeledNode, int levelsUntilStop = 0)
        {
            var fragmentString = $"({labeledNode.LTType})";

            if (labeledNode.Children.Count > 0)
            {
                var childrenString = "";

                var nextLevelsUntilStop = levelsUntilStop - 1;
                foreach (var child in labeledNode.Children)
                {
                    if ((child.IsFragmentRoot && levelsUntilStop < 1) || !child.CanHaveType)
                    {
                        childrenString += $"({child.LTType}) ";
                    }
                    else
                    {
                        childrenString += GetFragmentString(child, nextLevelsUntilStop);
                    }
                }

                fragmentString = $"({labeledNode.LTType} {childrenString} ) ";
            }

            return fragmentString;
        }

        public static string GetFragmentAndSurroundingsString(LabeledTreeNode fragmentRoot, int levelsAboveRoot, int levelsBelowRoot)
        {
            var ancestorSearchIterator = levelsAboveRoot;
            var ancestorNode = fragmentRoot;
            while (ancestorNode.Parent != null && ancestorSearchIterator > 0)
            {
                ancestorSearchIterator--;
                ancestorNode = ancestorNode.Parent;
            }

            var levelsTotal = levelsAboveRoot + levelsBelowRoot;

            return GetFragmentString(ancestorNode, levelsTotal);
        }

        public void CopySimpleProperties(LabeledTreeNode labeledNode)
        {
            if (labeledNode != null)
            {
                LTType = labeledNode.LTType;
                CanHaveType = labeledNode.CanHaveType;
                IsFragmentRoot = labeledNode.IsFragmentRoot;
            }
        }

        public override string ToString()
        {
            return LTType;
        }

        public static LabeledTreeNode FindFullFragmentRoot(LabeledTreeNode labeledNode)
        {
            if (labeledNode.Parent != null)
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

            while (!fragmentRoot.IsFragmentRoot)
            {
                if (fragmentRoot.LTType == "8840") // 8840 == CompilationUnit
                {
                    return fragmentRoot;
                }

                fragmentRoot = fragmentRoot.Parent;
            }

            return fragmentRoot;
        }
    }
}
