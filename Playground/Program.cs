using IdiomHtmlVisualizer;
using Microsoft.CodeAnalysis.CSharp;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS.CSTrees;
using RoseLibML.Util;
using System.Security.Cryptography;
using System.Text;

namespace Playground
{
    internal class Program
    {

        static Dictionary<string, List<LabeledNode>> idiomRoots = new Dictionary<string, List<LabeledNode>>();
        public static void Main(string[] args)
        {
            string code1;
            using (var streamReader = new StreamReader("Resources/Class1.cs"))
            {
                code1 = streamReader.ReadToEnd();
            }

            var syntaxTree1 = CSharpSyntaxTree.ParseText(code1);

            var labeledTree1 = CSTreeCreator.CreateTree(code1, new FixedNodeKinds() { FixedCutNodeKinds = new List<string> { "8842", "8855", "8875", "8873", "8878", "8892" } });
            LabeledTreeTransformations.Binarize(labeledTree1.Root, new CSNodeCreator(new FixedNodeKinds() { FixedCutNodeKinds = new List<string> { "8842", "8855", "8875", "8873", "8878", "8892" } }));
            labeledTree1.FilePath = "Resources/Class1.cs.bin";
            labeledTree1.Serialize();

            string code2;
            using (var streamReader = new StreamReader("Resources/Class2.cs"))
            {
                code2 = streamReader.ReadToEnd();
            }


            var syntaxTree2 = CSharpSyntaxTree.ParseText(code2);

            var labeledTree2 = CSTreeCreator.CreateTree(code2, new FixedNodeKinds() { FixedCutNodeKinds = new List<string> { "8842", "8855", "8875", "8873", "8878", "8892" } });
            LabeledTreeTransformations.Binarize(labeledTree2.Root, new CSNodeCreator(new FixedNodeKinds() { FixedCutNodeKinds = new List<string> { "8842", "8855", "8875", "8873", "8878", "8892" } }));
            labeledTree2.FilePath = "Resources/Class2.cs.bin";
            labeledTree2.Serialize();

            SortOutIdioms(new List<LabeledTree> { labeledTree1, labeledTree2 });
            MarkAllIdiomRootNodes();
            MarkIdiomNodes((labeledTree1.Root as CSNode)!);
            MarkIdiomNodes((labeledTree2.Root as CSNode)!);


            var dictionary1 = CreateSourceTargetMapping(labeledTree1);
            var dictionary2 = CreateSourceTargetMapping(labeledTree2);

            HtmlHelper.PrintHTML("Resources/idiomized1.htm", syntaxTree1, dictionary1);
            HtmlHelper.PrintHTML("Resources/idiomized2.htm", syntaxTree2, dictionary2);
        }

        public static void SortOutIdioms(List<LabeledTree> labeledTrees)
        {
            foreach (var labeledTree in labeledTrees)
            {
                FindIdioms(labeledTree.Root);
            }
        }

        public static void FindIdioms(LabeledNode node)
        {
            if (node.IsTreeRoot() || (node.IsFragmentRoot && !node.IsTreeLeaf))
            {
                var idiomString = node.GetFragmentString();
                if (!idiomRoots.ContainsKey(idiomString))
                {
                    idiomRoots[idiomString] = new List<LabeledNode>();
                }

                idiomRoots[idiomString].Add(node);
            }

            foreach (var child in node.Children)
            {
                FindIdioms(child);
            }
        }

        public static void MarkAllIdiomRootNodes()
        {
            foreach (var idiom in idiomRoots.Keys)
            {
                var list = idiomRoots[idiom];
                var newIdiomMark = Guid.NewGuid().ToString();
                foreach (var rootNode in list)
                {
                    rootNode.IdiomMark = newIdiomMark;
                }
            }
        }

        public static void MarkIdiomNodes(CSNode node)
        {
            if (!(node.IsTreeRoot() || (node.IsFragmentRoot && !node.IsTreeLeaf)))
            {
                node.IdiomMark = node.Parent.IdiomMark;
            }

            foreach (var child in node.Children)
            {
                MarkIdiomNodes((child as CSNode)!);
            }
        }

        public static Dictionary<uint, string> CreateSourceTargetMapping(CSTree labeledTree)
        {
            var root = labeledTree.Root;


            var dictionary = CreateDictionaryToFastenAccess(labeledTree);
            return dictionary;

        }

        public static Dictionary<uint, string> CreateDictionaryToFastenAccess(CSTree labeledTree)
        {
            var dictionary = new Dictionary<uint, string>();

            // Find all tree leafs and add them to dictionary
            var rootNode = labeledTree.Root as CSNode;


            ConsiderAddingToDictionary(dictionary, rootNode);


            return dictionary;
        }

        public static void ConsiderAddingToDictionary(Dictionary<uint, string> dictionary, CSNode node)
        {
            if (node.IsTreeLeaf)
            {
                byte[] encoded = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes($"{node.STInfo}|{node.RoslynSpanStart}|{node.RoslynSpanEnd}"));
                var value = BitConverter.ToUInt32(encoded, 0) % 1000000000;
                dictionary.Add(value, node.IdiomMark);

                return;
            }

            if (node.Children != null && node.Children.Count > 0)
            {
                foreach (var child in node.Children)
                {
                    ConsiderAddingToDictionary(dictionary, child as CSNode);
                }
            }
        }

    }
}