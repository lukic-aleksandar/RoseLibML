﻿using System;
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

            if (!UsedTypes.ContainsKey(type.GetQuasiUniqueRepresentation()))
            {
                TypeNodes.Add(type, new List<LabeledNode>(10));
                UsedTypes.Add(type.GetQuasiUniqueRepresentation(), type);
                node.Type = type;
            }
            else
            {
                node.Type = UsedTypes[type.GetQuasiUniqueRepresentation()];
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
                UsedTypes.Remove(currentType.GetQuasiUniqueRepresentation());
            }
        }

        /*

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

        private void MergeTypeNodes(Dictionary<LabeledNodeType, List<LabeledNode>> second)
        {
            foreach (var keyValuePair in second)
            {
                if (!TypeNodes.ContainsKey(keyValuePair.Key))
                {
                    TypeNodes.Add(keyValuePair.Key, new List<LabeledNode>());
                }

                var existingType = TypeNodes.Keys.Where(k => k.Equals(keyValuePair.Key)).FirstOrDefault();

                foreach (var node in keyValuePair.Value)
                {
                    node.Type = existingType;
                }

                TypeNodes[keyValuePair.Key].AddRange(keyValuePair.Value);
            }
        }

        */
    }
}
