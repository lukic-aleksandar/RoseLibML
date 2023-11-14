using RoseLibML.Core.LabeledTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.Core
{
    public class FileGrouper
    {
        // Klasa bi trebalo da ima isti fragment2
        // Neki od listova bi trebalo da ima isti fragment 2, i to po mogucnosti da bude istog tipa
        // Dužina tog fragmenta ti je možda isto interesantna
        // Dakle! Mislim da se nešta može odraditi! :) 
        // Nađem sve listove za koren
        // Uporedim njihove tipove i dužine sledstvenog fragmenta
        // I to je to, dobra osnova za grupisanje
        public static void GroupFilesBasedOnClassIdioms(TBSampler sampler)//Threshold - number of same adjacent idioms
        {
            // Extract class nodes
            var nodesOfAKind = ExtractNodesOfKind(sampler, "8855");
            var correspondingLeafCutNodes = ExtractLeafCutNodeTypes(nodesOfAKind);

            var typesAndRelatedClassNodes = new Dictionary<LabeledNodeType, List<LabeledNode>>();

            for (int i = 0; i < nodesOfAKind.Count; i++)
            {
                var leafCutNodes = correspondingLeafCutNodes[i];

                foreach (var leafCutNode in leafCutNodes)
                {
                    if (!typesAndRelatedClassNodes.ContainsKey(leafCutNode.Type))
                    {
                        typesAndRelatedClassNodes.Add(leafCutNode.Type, new List<LabeledNode>());
                    }

                    typesAndRelatedClassNodes[leafCutNode.Type].Add(leafCutNode);
                }
            }

            var typeFiles = new Dictionary<LabeledNodeType, List<string>>();

            // Za početak - neka bude samo tipovi. A onda i čitava podstabla koja odgovaraju tipu! :) 
            // sledeći korak bi bio da se nađu svi fajlovi gde su ista podstabla.
            foreach (var type in typesAndRelatedClassNodes.Keys)
            {
                if (typesAndRelatedClassNodes[type].Count < 4) { continue; } 
                else
                {
                    typeFiles[type] = new List<string>();
                    foreach (var node in typesAndRelatedClassNodes[type])
                    {
                        var rootNode = node.RootAncestor;

                        var tree = sampler.Trees
                            .Where(t => ReferenceEquals(t.Root, rootNode))
                            .First();

                        typeFiles[type].Add(tree.SourceFilePath);
                    }
                }
            }
        }

        // Only the first node of a kind gets extracted - address later
        private static List<LabeledNode> ExtractNodesOfKind(TBSampler sampler, string STInfo)
        {
            var nodes = new List<LabeledNode>();
            foreach (var tree in sampler.Trees)
            {

                var rootNode = FindFirstNodeWithSTInfo(tree.Root, STInfo);

                if (rootNode != null)
                {
                    Console.WriteLine();
                    Console.WriteLine($"{rootNode.STInfo} - type part 2 {rootNode.Type.Part2Fragment}");

                    nodes.Add(rootNode);
                }

            }

            return nodes;
        }

        static LabeledNode? FindFirstNodeWithSTInfo(LabeledNode node, string STInfo)
        {
            if (node == null) return null;

            if (node.STInfo == STInfo) return node;

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {

                    var foundClassNode = FindFirstNodeWithSTInfo(child, "8855");
                    if (foundClassNode != null) { return foundClassNode; }

                }
            }

            return null;
        }

        private static List<List<LabeledNode>> ExtractLeafCutNodeTypes(List<LabeledNode> nodes)
        {
            List<List<LabeledNode>> nodeLeaves = new List<List<LabeledNode>>();

            foreach (var node in nodes)
            {
                var allLeaves = node.Children[0].GetAllFullFragmentLeaves();
                var filteredLeaves = allLeaves.Where(leaf => leaf.IsTreeLeaf == false
                    && leaf.IsFragmentRoot == true
                    && leaf.IsFixed == false
                    && leaf.CanHaveType == true).ToList();
                nodeLeaves.Add(filteredLeaves);
            }

            return nodeLeaves;
        }
    }
}
