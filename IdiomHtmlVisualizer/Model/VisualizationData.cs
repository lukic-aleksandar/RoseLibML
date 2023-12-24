using Microsoft.CodeAnalysis;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS.CSTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdiomHtmlVisualizer.Model
{
    public class VisualizationData
    {
        public string HtmlFileName { get; set; }
        public SyntaxTree SourceSyntaxTree { get; set; }
        public CSTree LabeledTree { get; set; }
        public Dictionary<uint, string> Source2TargetMapping { get; set; }

        public IdiomHandler IdiomHandler { get; set; }

        public VisualizationData(
            string htmlFile,
            SyntaxTree syntaxTree,
            CSTree labeledTree,
            IdiomHandler idiomHandler)
        {
            HtmlFileName = htmlFile;
            SourceSyntaxTree = syntaxTree;
            LabeledTree = labeledTree;
            IdiomHandler = idiomHandler;
            Source2TargetMapping = CreateSource2TargetMapping();
        }

        public Dictionary<uint, string> CreateSource2TargetMapping()
        {
            var dictionary = new Dictionary<uint, string>();
            var rootNode = LabeledTree.Root as CSNode;

            ConsiderAddingToDictionary(dictionary, rootNode!);
            return dictionary;
        }

        public void ConsiderAddingToDictionary(Dictionary<uint, string> dictionary, CSNode node)
        {
            if (node.IsTreeLeaf)
            {
                uint nodeHashValue = NodeHasher.CalculateNodeHash(node.STInfo, node.RoslynSpanStart, node.RoslynSpanEnd);
                dictionary.Add(nodeHashValue, node.IdiomMark);

                return;
            }

            if (node.Children != null && node.Children.Count > 0)
            {
                foreach (var child in node.Children)
                {
                    ConsiderAddingToDictionary(dictionary, (child as CSNode)!);
                }
            }
        }

    }
}
