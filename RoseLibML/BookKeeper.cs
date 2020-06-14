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
        public Dictionary<LabeledTreeNodeType, List<LabeledTreeNode>> TypeNodes { get; set; }

        public BookKeeper()
        {
            FragmentCounts = new Dictionary<string, int>();
            RootCounts = new Dictionary<string, int>();
            TypeNodes = new Dictionary<LabeledTreeNodeType, List<LabeledTreeNode>>();
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

        public void AddNodeType(LabeledTreeNodeType type, LabeledTreeNode node)
        {
            if (!TypeNodes.ContainsKey(type))
            {
                TypeNodes.Add(type, new List<LabeledTreeNode>());
            }

            TypeNodes[type].Add(node);
        }

        public void RemoveNodeType(LabeledTreeNodeType type, LabeledTreeNode node)
        {
            if (!TypeNodes.ContainsKey(type))
            {
                TypeNodes.Add(type, new List<LabeledTreeNode>());
            }
            else
            {
                TypeNodes[type].Remove(node);
            }
        }

        public void Merge(BookKeeper bookKeeper)
        {
            MergeCountDictionaries(FragmentCounts, bookKeeper.FragmentCounts);
            MergeCountDictionaries(RootCounts, bookKeeper.RootCounts);
            MergeTypeNodes(bookKeeper.TypeNodes);
        }

        private void MergeCountDictionaries(Dictionary<string, int> first, Dictionary<string, int> second)
        {
            foreach (var keyValuePair in second)
            {
                if (!first.ContainsKey(keyValuePair.Key))
                {
                    first.Add(keyValuePair.Key, 0);
                }

                first[keyValuePair.Key] += keyValuePair.Value;
            }
        }

        private void MergeTypeNodes(Dictionary<LabeledTreeNodeType, List<LabeledTreeNode>> second)
        {
            foreach (var keyValuePair in second)
            {
                if (!TypeNodes.ContainsKey(keyValuePair.Key))
                {
                    TypeNodes.Add(keyValuePair.Key, new List<LabeledTreeNode>());
                }

                TypeNodes[keyValuePair.Key].AddRange(keyValuePair.Value);
            }
        }
    }
}
