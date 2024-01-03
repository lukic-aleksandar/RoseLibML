using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS.CSTrees;
using RoseLibML.Util;
using System.Text;
using System.Security.Cryptography;
using MathNet.Numerics;
using System.Xml.Linq;
using System.IO;
using System;
using IdiomHtmlVisualizer.Model;

namespace IdiomHtmlVisualizer
{
    // Dobro... Dodao si popup, čist HTML-CSS popup
    // Ostaje ti da rešiš po koju drugu stvar!
    // Info koji će se u tom popupu prikazivati
    // Imaš po koju kontekstnu info - broj idioma, broj podstabala
    // Ostaju ti linkovi
    // Tu je mala zavrzlama :o 
    // Ta zavrzlama je sledeća! Id do elementa
    // Ideja je da rešiš pomoću konvencije, idiom mark + id
    // Uh, ovo neće raditi za tree fragmente :o nemaš ništa što ih jedinstveno određuje
    // Ali na isti način bi mogao i njih da rešiš, ja bih rekao :) Mada, uh, nije tako lako :D Pogotovo zbog ID-a...
    // Nema veze, rešiš za idiome i kraj, gotovo :) Ne moraš baš sve da rešiš...
    // Tako da, samo idiomi
    // Potreban ti je ukupan broj tog idioma po fajlu. To je malo zeznuto, koja struktura to da čuva.
    // Huh, an idea popped up! :) string.Concat(idiomMark, fileName) 

    internal class Program
    {

        static Dictionary<string, (FileInfo syntaxTreeFI, FileInfo csTreeFI)> counterpartsPaths = new Dictionary<string, (FileInfo syntaxTreeFI, FileInfo CSTreeFI)>();
        static Dictionary<string, (SyntaxTree syntaxTree, CSTree csTree)> counterpartsTrees = new Dictionary<string, (SyntaxTree syntaxTree, CSTree csTree)>();
        static void Main(string[] args)
        {

            if (!EnsurePathArguments(args))
            {
                return;
            }

            var csFilesPath = args[0];
            var binFilesPath = args[1];
            var outputPath = args[2];

            var provideComprehensiveReport = false;
            var idiomLengthThreshold = 0;
            if(args.Length > 3)
            {
                if (!EnsureOptionalArguments(args))
                {
                    return;
                }
                provideComprehensiveReport = true;
                idiomLengthThreshold = int.Parse(args[4]);
            }
            

            LoadCounterpartsPaths(csFilesPath, binFilesPath);
            LoadCounterpartsTrees();


            PrepareDataGenerateHtmlFiles(outputPath, provideComprehensiveReport, idiomLengthThreshold);
        }

        private static bool EnsurePathArguments(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Three parameters are required - (existing) paths for a folder with .cs files and a folder with their bin counterparts, and an output path for HTML files");
                Console.ReadKey();
                return false;
            }


            var csFilesPath = args[0];
            var binFilesPath = args[1];

            var csFilesPathExists = Directory.Exists(csFilesPath);
            var binFilesPathExists = Directory.Exists(binFilesPath);

            if (!(csFilesPathExists && binFilesPathExists))
            {
                Console.WriteLine("Two parameters are required - (existing) paths for a folder with .cs files and a folder with their bin counterparts");
                Console.ReadKey();
                return false;
            }

            return true;
        }

        private static bool EnsureOptionalArguments(string[] args)
        {   
            // No optional parameters
            if(args.Length <= 3) 
            {
                return true;
            }

            // If there is another (not mandatory) argument, see if it is -c
            if (!args[3].Equals("-c"))
            {
                Console.WriteLine("The only optional parameter available is '-c', for comprehensive reporting");
                Console.ReadKey();
                return false;
            }

            if(args.Length != 5)
            {
                Console.WriteLine("The optional parameter '-c' for comprehensive reporting must be followed by a threshold (int) for the minimum lenght of analyzed idioms");
                Console.ReadKey();
                return false;
            }

            if(int.TryParse(args[3], out int threshold))
            {
                Console.WriteLine("The optional parameter '-c' for comprehensive reporting must be followed by a threshold (int) for the minimum lenght of analyzed idioms");
                Console.ReadKey();
                return false;
            }


            return true;
        }

        private static void LoadCounterpartsPaths(string csFilesPath, string binFilesPath)
        {
            var csDirectoryInfo = new DirectoryInfo(csFilesPath);
            var binDirectoryInfo = new DirectoryInfo(binFilesPath);

            var csFiles = csDirectoryInfo.GetFiles("*.cs");
            var binFiles = binDirectoryInfo.GetFiles("*.bin");

            foreach ( var csFileInfo in csFiles )
            {
                var fileName = Path.GetFileNameWithoutExtension(csFileInfo.Name);
                var binFileInfo = binFiles
                                    .Where(b => Path.GetFileNameWithoutExtension(b.Name) == csFileInfo.Name)
                                    .FirstOrDefault();
                if(binFileInfo == null)
                {
                    throw new DataMisalignedException("For some reason, a .cs file does not have it's binary counterpart");
                }


                (FileInfo syntaxTreeFI, FileInfo CSTreeFI) counterparts = (csFileInfo, binFileInfo);
                counterpartsPaths.Add(fileName, counterparts);
            }
        }

        private static void LoadCounterpartsTrees()
        {
            foreach (var fileName in counterpartsPaths.Keys)
            {
                using (var streamReader = new StreamReader(counterpartsPaths[fileName].syntaxTreeFI.FullName))
                {
                    var code = streamReader.ReadToEnd();
                    var syntaxTree = CSharpSyntaxTree.ParseText(code);

                    var labeledTree = CSTreeCreator.Deserialize(counterpartsPaths[fileName].syntaxTreeFI.FullName, counterpartsPaths[fileName].csTreeFI.FullName, fileName);
                    if (labeledTree == null)
                    {
                        throw new FileNotFoundException($"A problem with deserializing file {counterpartsPaths[fileName].csTreeFI}.");
                    }

                    (SyntaxTree syntaxTree, CSTree csTree) treesTuple = (syntaxTree, labeledTree);
                    counterpartsTrees.Add(fileName, treesTuple);
                }
            }
        }

        private static void PrepareDataGenerateHtmlFiles(string outputDir, bool provideComprehensiveReport, int idiomLengthThreshold)
        {
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var labeledTrees = counterpartsTrees.Values.Select(t => t.csTree).Cast<LabeledTree>().ToList();
            
            var idiomHandler = new IdiomHandler(labeledTrees);
            idiomHandler.SortOutIdioms();
            idiomHandler.MarkAllIdiomRootNodes();
            idiomHandler.GenerateCodeFragmentsForAllIdioms();

            if (provideComprehensiveReport)
            {
                idiomHandler.SortOutSubtreesBasedOnIdioms(idiomLengthThreshold);
            }

            foreach (var fileName in counterpartsTrees.Keys)
            {
                idiomHandler.MarkIdiomNodes((counterpartsTrees[fileName].csTree.Root as CSNode)!);
                var visualizationData = new VisualizationData(
                        Path.Combine(outputDir, $"{fileName}.htm"),
                        counterpartsTrees[fileName].syntaxTree,
                        counterpartsTrees[fileName].csTree,
                        idiomHandler
                    );
                HtmlGenerator.GenerateHTML(visualizationData);
            }
        }
    }
}