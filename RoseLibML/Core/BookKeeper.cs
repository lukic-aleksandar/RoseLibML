using RoseLibML.Core.LabeledTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML
{
    public class BookKeeper
    {
        public Dictionary<string, int> FragmentCounts { get; set; }
        public Dictionary<string, int> RootCounts { get; set; }
        public Dictionary<LabeledNodeType, List<LabeledNode>> TypeNodes { get; set; }
        public Dictionary<string, LabeledNodeType> UsedTypes { get; set; }

        public BookKeeper()
        {
            FragmentCounts = new Dictionary<string, int>();
            RootCounts = new Dictionary<string, int>();
            TypeNodes = new Dictionary<LabeledNodeType, List<LabeledNode>>();
            UsedTypes = new Dictionary<string, LabeledNodeType>();
        }


        public int GetFragmentCount(string fragment)
        {
            if (!FragmentCounts.ContainsKey(fragment))
            {
                return 0;
            }

            return FragmentCounts[fragment];
        }

        public void IncrementFragmentCount(string fragment, int count = 1)
        {
            if (!FragmentCounts.ContainsKey(fragment))
            {
                FragmentCounts.Add(fragment, 0);
            }

            FragmentCounts[fragment] += count;
        }

        public void DecrementFragmentCount(string fragment, int count = 1)
        {
            if (!FragmentCounts.ContainsKey(fragment))
            {
                FragmentCounts.Add(fragment, 0);
            }

            FragmentCounts[fragment] -= count;

            if (FragmentCounts[fragment] < 0)
            {
                FragmentCounts[fragment] = 0;
            }
        }

        public int GetRootCount(string root)
        {
            if (!RootCounts.ContainsKey(root))
            {
                return 0;
            }

            return RootCounts[root];
        }

        public void IncrementRootCount(string root, int count = 1)
        {
            if (!RootCounts.ContainsKey(root))
            {
                RootCounts.Add(root, 0);
            }

            RootCounts[root] += count;
        }

        public void DecrementRootCount(string root, int count = 1)
        {
            if (!RootCounts.ContainsKey(root))
            {
                RootCounts.Add(root, 0);
            }

            RootCounts[root] -= count;

            if (RootCounts[root] < 0)
            {
                RootCounts[root] = 0;
            }
        }

        public virtual void AddNodeType(LabeledNodeType type, LabeledNode node)
        {

            if (!UsedTypes.ContainsKey(type.GetTypeHash()))
            {
                TypeNodes.Add(type, new List<LabeledNode>(10));
                UsedTypes.Add(type.GetTypeHash(), type);
                node.Type = type;
            }
            else
            {
                node.Type = UsedTypes[type.GetTypeHash()];
            }

            TypeNodes[node.Type].Add(node);
        }

        public void RemoveNodeType(LabeledNodeType type, LabeledNode node)
        {
            if (TypeNodes.ContainsKey(type))
            {
                TypeNodes[type].Remove(node);
            }
        }

        public void RemoveZeroNodeTypes()
        {
            var zeroNodeTypes = TypeNodes.Keys.Where(k => TypeNodes[k].Count == 0).ToList();

            for(var i = zeroNodeTypes.Count() - 1; i >= 0; i--)
            {
                var currentType = zeroNodeTypes[i];
                TypeNodes.Remove(currentType);
                UsedTypes.Remove(currentType.GetTypeHash());
            }
        }

        public void RecordTreeData(LabeledTree labeledTree)
        {
            foreach (var child in labeledTree.Root.Children) // Skips the root
            {
                RecordRootsFragmentsTypes(child);
            }
        }

        private void RecordRootsFragmentsTypes(LabeledNode node)
        {
            if (node.IsFragmentRoot)
            {
                IncrementRootCount(node.STInfo);
                IncrementFragmentCount(LabeledNodeType.CalculateFragmentHash(node.GetFragmentString()));
            }

            if (node.CanHaveType)
            {
                AddNodeType(LabeledNode.GetType(node), node);
            }

            foreach (var child in node.Children)
            {
                RecordRootsFragmentsTypes(child);
            }
        }
    }
}
