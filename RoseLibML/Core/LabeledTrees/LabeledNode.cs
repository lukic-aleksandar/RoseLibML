﻿using MathNet.Numerics.Distributions;
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

namespace RoseLibML.Core.LabeledTrees
{
    [Serializable]
    public abstract class LabeledNode
    {
        public abstract bool IsTreeRoot();
        public bool IsFixed { get; set; } = false;
        public bool IsTreeLeaf { get; set; } = false; // In C# terms - a token
        public string STInfo { get; set; } // Syntax Tree Info - serves to hold Concrete Syntax type, some concrete value, or custom type.
        public LabeledNode Parent { get; set; }
        public List<LabeledNode> Children { get; set; } = new List<LabeledNode>();

        public LabeledNode RootAncestor { get
            {
                if (Parent == null)
                {
                    return this;
                }

                var currentNode = Parent;
                while(currentNode.Parent != null) { 
                    currentNode = currentNode.Parent;
                }

                return currentNode;
            } 
        }

        public bool CanHaveType { get; set; } = true;

        [NonSerialized]
        private LabeledNodeType type;
        public LabeledNodeType Type
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


        [NonSerialized]
        private (string typeCode, short iteration) lastModified = (typeCode: "", iteration: -1);
        public (string typeCode, short iteration) LastModified { get => lastModified; set { lastModified = value; } }


        public abstract LabeledNode CreateSimpleDuplicate();

        public void AddChild(LabeledNode child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public static LabeledNodeType GetType(LabeledNode labeledNode)
        {
            var oldIsFragmentRoot = labeledNode.IsFragmentRoot;
            labeledNode.IsFragmentRoot = false;
            var fragmentRoot = labeledNode.FindFragmentRoot();
            var full = fragmentRoot.GetFragmentString();
            labeledNode.IsFragmentRoot = true;
            var part1 = fragmentRoot.GetFragmentString();
            var part2 = labeledNode.GetFragmentString();
            labeledNode.IsFragmentRoot = oldIsFragmentRoot;

            return new LabeledNodeType()
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

        // Currently used only for HTML Vizualization
        #region HTMLVizualization
        public string? IdiomMark { get; set; }
        #endregion
    }
}
