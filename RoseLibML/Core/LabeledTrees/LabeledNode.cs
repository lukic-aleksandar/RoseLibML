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
using RoseLibML.Core.LabeledTrees;

namespace RoseLibML
{
    [Serializable]
    public class LabeledNode
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
        public string STInfo { get; set; } // Syntax Tree Info - serves to hold Concrete Syntax type, some concrete value, or custom type.
        
        public LabeledNode Parent { get; set; }
        public List<LabeledNode> Children { get; set; } = new List<LabeledNode>();

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

        public void CopySimpleProperties(LabeledNode labeledNode)
        {
            if (labeledNode != null)
            {
                STInfo = labeledNode.STInfo;
                CanHaveType = labeledNode.CanHaveType;
                IsFragmentRoot = labeledNode.IsFragmentRoot;
            }
        }

        public void AddChild(LabeledNode child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public static LabeledTreeNodeType GetType(LabeledNode labeledNode)
        {
            var oldIsFragmentRoot = labeledNode.IsFragmentRoot;
            labeledNode.IsFragmentRoot = false;
            var fragmentRoot = labeledNode.FindFragmentRoot();
            var full = fragmentRoot.GetFragmentString();
            labeledNode.IsFragmentRoot = true;
            var part1 = fragmentRoot.GetFragmentString();
            var part2 = labeledNode.GetFragmentString();
            labeledNode.IsFragmentRoot = oldIsFragmentRoot;

            return new LabeledTreeNodeType()
            {
                FullFragment = full,
                Part1Fragment = part1,
                Part2Fragment = part2
            };
        }

        public override string ToString()
        {
            return STInfo;
        }


        public void Serialize(string filePath)
        {
            BinaryFormatter b = new BinaryFormatter();
            var fileStream = File.Create(filePath);
            b.Serialize(fileStream, this);
            fileStream.Close();
        }

        public static LabeledNode Deserialize(string filePath)
        {
            try
            {
                BinaryFormatter b = new BinaryFormatter();
                var fileStream = File.OpenRead(filePath);
                var treeNode = (LabeledNode)b.Deserialize(fileStream);
                fileStream.Close();
                return treeNode;
            }
            catch
            {

            }

            return null;
        }



        public bool UseRoslynMatchToWrite { get; set; }
        public int RoslynSpanStart { get; set; }
        public int RoslynSpanEnd { get; set; }

        public bool IsExistingRoslynNode
        {
            get { return ushort.TryParse(this.STInfo, out ushort result); }
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

        
        
    }
}
