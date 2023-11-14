using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS.CSTrees;
using RoseLibML.Util;
using System.Text;
using System.Security.Cryptography;

namespace IdiomHtmlVisualizer
{
    internal class Program
    {

        static Dictionary<string, ushort> idiomColors = new Dictionary<string, ushort>();
        static void Main(string[] args)
        {
            var code = @"
                namespace Test{
                    public class MyClass
                    {
                        public void MyMethod()
                        {
                        }
                    }
                }
            ";

            var syntaxTree = CSharpSyntaxTree.ParseText(code);


            var labeledTree = CSTreeCreator.CreateTree(code, new FixedNodeKinds() { FixedCutNodeKinds = new List<string> { "8842", "8855", "8875", "8873", "8878", "8892" } });
            LabeledTreeTransformations.Binarize(labeledTree.Root, new CSNodeCreator(new FixedNodeKinds() { FixedCutNodeKinds = new List<string> { "8842", "8855", "8875", "8873", "8878", "8892" } }));

            var dictionary = CreateSourceTargetMapping(labeledTree);

            PrintHTML(syntaxTree, dictionary);


            // Kako sad proći kroz binarizovano stablo, i naći idiome? Možeš naći fragmente...
            // Pa jbg, rekurzija i obeleži sve čvorove. Ideš jedan po jedan čvor, nije da je tako strašno.
            // Kome pripadaju čvorovi na "granici, cut-ovi? Svejedno je. Mene interesuju samo tree leaf-ovi.
            // Kako mapiraš taj čvor na čvor u bin temp. Hash mapa? Napravi strukturu za to. Tip čvora + lokacija?
            // Mislim da svaki token postoji u oba stabla, to je pretpostavka na osnovu koje radi...
        }

        private static void PrintHTML(SyntaxTree syntaxTree, Dictionary<uint, string> dictionary)
        {
            var tokens = syntaxTree.GetRoot().DescendantTokens();
            using (var streamWriter = new StreamWriter("tokens.htm"))
            {
                streamWriter.Write(@"
<html>
<head>
    <style>span {white-space: pre ; font-family:'Courier New';}</style>
<style>
        #tooltip {
          background-color: #333;
          color: white;
          padding: 5px 10px;
          border-radius: 4px;
          font-size: 13px;
          display: none;
        }

        #arrow,
        #arrow::before {
            position: absolute;
            width: 8px;
            height: 8px;
            background: inherit;
        }

        #arrow {
            visibility: hidden;
        }

        #arrow::before {
            visibility: visible;
            content: '';
            transform: rotate(45deg);
        }

        #tooltip[data-popper-placement^='top'] > #arrow {
            bottom: -4px;
        }

        #tooltip[data-popper-placement^='bottom'] > #arrow {
            top: -4px;
        }

        #tooltip[data-popper-placement^='left'] > #arrow {
            right: -4px;
        }

        #tooltip[data-popper-placement^='right'] > #arrow {
            left: -4px;
        }

        #tooltip[data-show] {
            display: block;
        }
    </style>
    <script>
        function highlight(x) {
            let className = x.className;
            let previousColor = x.style.backgroundColor;

            localStorage.setItem(""previousColor"", previousColor);

            let idiomElements = document.getElementsByClassName(className);

            for(var i=0;i<idiomElements.length;i++){
                idiomElements[i].style.backgroundColor = '#257AFD';
            }

            const tooltipText = document.querySelector('#tooltip-text');
            tooltipText.textContent = className;
            const tooltip = document.querySelector('#tooltip');
            Popper.createPopper(x, tooltip , {
                modifiers: [
                    {
                    name: 'offset',
                    options: {
                        offset: [0, 8],
                    },
                    },
                ],
            });
            tooltip.setAttribute('data-show', '');
        }

        function unhighlight(x) {
            let className = x.className;

            var previousColor = localStorage.getItem(""previousColor"", previousColor);

            let idiomElements = document.getElementsByClassName(className);

            for(var i=0;i<idiomElements.length;i++){
                idiomElements[i].style.backgroundColor = previousColor;
            }

            const tooltip = document.querySelector('#tooltip');
            tooltip.removeAttribute('data-show' );
        }
        
        function selectIdiom(x) {
            let className = x.className;

            let idiomElements = document.getElementsByClassName(className);

            let idiom = ``;
            for(var i=0;i<idiomElements.length;i++){
                idiom += idiomElements[i].textContent;
            }

            alert(idiom);
        }
    </script>
</head>
<body>
");
                var previousTokenGuid = string.Empty;
                foreach (var token in tokens)
                {
                    var STInfo = token.ValueText;
                    var RoslynSpanStart = token.Span.Start;
                    var RoslynSpanEnd = token.Span.End;

                    byte[] encoded = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes($"{STInfo}|{RoslynSpanStart}|{RoslynSpanEnd}"));
                    var keyValue = BitConverter.ToUInt32(encoded, 0) % 1000000000;

                    var tokenGuid = dictionary.GetValueOrDefault(keyValue);
                    if (tokenGuid == null)
                    {
                        throw new DataMisalignedException("For some reason, tokenGuid not found :(");
                    }

                    if (tokenGuid == previousTokenGuid)
                    {
                        streamWriter.Write($"{token.ToFullString()}");
                    }
                    else
                    {
                        if (previousTokenGuid != string.Empty)
                        {
                            streamWriter.Write($"</span>");
                        }
                        streamWriter.Write($"<span style=\"background-color:{ChooseColor(GetColorNumber(tokenGuid))};\" class=\"{tokenGuid}\" onmouseover=\"highlight(this)\" onmouseout=\"unhighlight(this)\" onclick=\"selectIdiom(this)\">{token.ToFullString()}");
                        previousTokenGuid = tokenGuid;
                    }
                }
                streamWriter.Write(@"
</span>
<div id=""tooltip"" role=""tooltip"">
    <span id=""tooltip-text""></span>
    <div id=""arrow"" data-popper-arrow></div>
</div>
<script src=""https://unpkg.com/@popperjs/core@2/dist/umd/popper.js""></script>
</body>
</html>
");
            }
        }

        public static Dictionary<uint, string> CreateSourceTargetMapping(CSTree labeledTree)
        {
            var root = labeledTree.Root;

            MarkIdiomNodes(root as CSNode);
            var dictionary = CreateDictionaryToFastenAccess(labeledTree);
            return dictionary;

        }

        // Jedan idiom, u jednom stablu
        // Fora je što, za sad, nemam mogućnost da prepoznam drugi, isti takav idiom
        // i u drugom stablu
        // Međutim! 
        // Možda bih na osnovu tipova i mogao?
        // Možda, ali hajde za početak jedan fajl, pa proširenje na više različitih fajlova.
        public static void MarkIdiomNodes(CSNode node)
        {
            if (node.IsTreeRoot() || (node.IsFragmentRoot && !node.IsTreeLeaf))
            {
                // TODO: Morao si u čvor da dodaš idiom mark
                node.IdiomMark = Guid.NewGuid().ToString();
            }
            else
            {
                node.IdiomMark = node.Parent.IdiomMark;
            }
            foreach (var child in node.Children)
            {
                MarkIdiomNodes(child as CSNode);
            }
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
                // TODO: Morao si u NodeCreator za identifier da dodaš start i end 
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

        public static ushort GetColorNumber(string idiomMark)
        {
            var colorNumber = idiomColors.GetValueOrDefault(idiomMark);
            if (colorNumber == 0)
            {
                colorNumber = (ushort)new Random().NextInt64();
                idiomColors.Add(idiomMark, colorNumber);
            }

            return colorNumber;
        }

        public static string ChooseColor(int number)
        {
            var hue = number * 137.508; // use golden angle approximation
            return $"hsl({hue},50%,75%)";
        }
    }

}