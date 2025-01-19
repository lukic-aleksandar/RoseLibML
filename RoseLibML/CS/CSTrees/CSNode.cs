using RoseLibML.Core.LabeledTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RoseLibML.CS.CSTrees
{
    [Serializable]
    public class CSNode : LabeledNode
    {
        public bool IsExistingRoslynNode
        {
            get { return ushort.TryParse(this.STInfo, out ushort result) || this.IsTreeLeaf; }
        }
        public bool UseRoslynMatchToWrite { get; set; }
        public int RoslynSpanStart { get; set; }
        public int RoslynSpanEnd { get; set; }


        // Možda bih ovo mogao da izmenim? To bi, možda, bilo dovoljno.
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
        public override bool IsTreeRoot()
        {
            if (STInfo == "8840") // 8840 == CompilationUnit
            {
                return true;
            }

            return false;
        }

        // Only fields that could be interesting for calculation
        public override LabeledNode CreateSimpleDuplicate()
        {
            var simpleDuplicate = new CSNode();
            simpleDuplicate.STInfo = STInfo;
            simpleDuplicate.CanHaveType = CanHaveType;
            simpleDuplicate.IsFragmentRoot = IsFragmentRoot;
            simpleDuplicate.IsTreeLeaf = IsTreeLeaf;
            simpleDuplicate.IsFixed = IsFixed;

            return simpleDuplicate;
        }

        // Creates a subtree from an idiom written in a treebank notation
        // The subtree does not contain all the info, just the info possible to discern from and idiom
        // This means position and other possible things are not filled in, or are filled based on some assumptions
        // Assumptions:
        // - Strings do not end with whitespaces (these get trimmed)
        // - Strings do not contain unescaped parenthesis ( ( and ) ), remove those in the preprocessing step
        // Cannot (at the moment):
        // - Discern whether a node is a leaf node
        public static CSNode CreateSubtreeFromIdiom(string idiom)
        {
            var sanitizedIdiom = idiom
                .Trim()
                .Replace("(()", "(%op%)")
                .Replace("())", "(%cp%)");

            var level = 0;
            var currentWord = "";

            var idiomReadyForDeserialization = sanitizedIdiom
                .Substring(1, sanitizedIdiom.Length - 2)
                .Trim();

            var currentNode = new CSNode();

            foreach (var ch in idiomReadyForDeserialization)
            {
                if (ch == '(')
                {
                    level++;
                    if (currentNode.STInfo == null || currentNode.STInfo.Length == 0)
                    {
                        currentNode.STInfo = currentWord.Trim();
                        if (currentNode.STInfo == "%op%") currentNode.STInfo = "(";
                        if (currentNode.STInfo == "%cp%") currentNode.STInfo = ")";
                    }
                    currentWord = "";

                    var newChildNode = new CSNode();

                    newChildNode.Parent = currentNode;
                    currentNode.Children.Add(newChildNode);

                    currentNode = newChildNode;
                    continue;
                }
                if (ch == ')')
                {
                    level--;

                    if (currentNode.STInfo == null || currentNode.STInfo.Length == 0)
                    {
                        currentNode.STInfo = currentWord.Trim();
                        if (currentNode.STInfo == "%op%") currentNode.STInfo = "(";
                        if (currentNode.STInfo == "%cp%") currentNode.STInfo = ")";
                    }
                    currentWord = "";

                    currentNode = (CSNode)currentNode.Parent;
                    continue;
                }

                currentWord += ch;
            }

            if (level != 0)
            {
                throw new Exception("Level when deserializing not 0!");
            }

            currentNode.IsFragmentRoot = true;
            currentNode.IsTreeLeaf = false;
            Stack<LabeledNode> hierarchyNodesStack = new Stack<LabeledNode>(currentNode.Children);
            while (hierarchyNodesStack.Count > 0)
            {
                LabeledNode node = hierarchyNodesStack.Pop();

                if (node.Children.Count == 0)
                {
                    node.IsFragmentRoot = true;
                }
                else
                {
                    node.Children.ForEach(cn => hierarchyNodesStack.Push(cn));
                }
            }

            return currentNode;
        }

    }
}
