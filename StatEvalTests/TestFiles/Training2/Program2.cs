namespace StatEvalDemo
{
    internal class Program
    {
        static Dictionary<string, (FileInfo syntaxTreeFI, FileInfo csTreeFI)> counterpartsPaths = new Dictionary<string, (FileInfo syntaxTreeFI, FileInfo CSTreeFI)>();
        static Dictionary<string, (SyntaxTree syntaxTree, CSTree csTree)> counterpartsTrees = new Dictionary<string, (SyntaxTree syntaxTree, CSTree csTree)>();

        // Needed
        // - both the source code and the corresponding binary files
        // of the training set
        // Convention - in data and out model folders
        // Configuration (for transformation purposes)
        // Test set of cs files that need to be transformed into labeled trees.
        static void da(string[] args)
        {
            ReadTrainingFiles("", "");

            var labeledTrees = counterpartsTrees.Values.Select(t => t.csTree).Cast<LabeledTree>().ToList();
            var idiomHandler = new IdiomHandler(labeledTrees);


            idiomHandler.SortOutIdiomsInTrainingSet();

            ReadTestFiles("");
            // CreateLabeledTrees iz RoseLibML Program klase
            //idiomHandler.FindIdenticalSubtrees(...); za svaki od idioma iznad datog threshold-a.
        }

        public static void ReadTrainingFiles(string inDataDir, string outModelDir)
        {
            LoadCounterpartsPaths(inDataDir, outModelDir);
            LoadCounterpartsTrees();
        }

        private static void LoadCounterpartsPaths(string csFilesPath, string binFilesPath)
        {
            var csDirectoryInfo = new DirectoryInfo(csFilesPath);
            var binDirectoryInfo = new DirectoryInfo(binFilesPath);

            var csFiles = csDirectoryInfo.GetFiles("*.cs");
            var binFiles = binDirectoryInfo.GetFiles("*.bin");

            foreach (var csFileInfo in csFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(csFileInfo.Name);
                var binFileInfo = binFiles
                                    .Where(b => Path.GetFileNameWithoutExtension(b.Name) == csFileInfo.Name)
                                    .FirstOrDefault();
                if (binFileInfo == null)
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

        private static void ReadTestFiles(string inDataDir)
        {
            // Read the CS file
            // Generate the bin file
        }
    }
}
