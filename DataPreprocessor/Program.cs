using DataPreprocessor;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Text;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Starting... :)");

        if (args.Length < 2)
        {
            Console.WriteLine("You need to provide two arguments: an input and an output path");
            return;
        }

        string inputPath = args[0];
        string outputPath = args[1];

        bool shouldGroup = args.Length == 3 ? ExtractShouldGroupFlagValue(args[2]) : false;

        bool inputExists = Directory.Exists(inputPath);
        bool outputExists = Directory.Exists(outputPath);

        if (!inputExists)
        {
            Console.WriteLine("Provided input path is not valid");
        }

        ProjectAnalyser analyser = new ProjectAnalyser();

        Dictionary<string, List<Tuple<FileInfo, string>>>? groupsForOutput = null;
        if (shouldGroup)
        {
            var componentGroups = analyser.AnalyseProject(inputPath);
            groupsForOutput = analyser.GetComponentGroupsWithoutBaseAndNonRepetative(2);
        }
        else
        {
            List<Tuple<FileInfo, string>> allGroupsCombined = new List<Tuple<FileInfo, string>>();
            
            //Expects a flat structure...
            var filePaths = Directory.GetFiles(inputPath);
            foreach (var filePath in filePaths)
            {
                var fi = new FileInfo(filePath);
                string text = "";
                using (var reader = new StreamReader(filePath))
                {
                    text = reader.ReadToEnd();
                }

                allGroupsCombined.Add(new Tuple<FileInfo, string>(fi, text));
            }
            
            groupsForOutput = new Dictionary<string, List<Tuple<FileInfo, string>>>();
            groupsForOutput.Add("all", allGroupsCombined);
        }

        WriteButRemoveCommentsAndUsings(outputPath, groupsForOutput);

        Console.WriteLine("Grouped, cleansed, and wrote all the files :)");
        Console.ReadKey();
    }

    private static bool ExtractShouldGroupFlagValue(string argument)
    {
        var flagParts = argument.Split('=');
        if(flagParts.Length == 2)
        {
            if (flagParts[0] == "--group")
            {
                return bool.Parse(flagParts[1]);
            }
        }

        throw new ArgumentException("Unknown argument: " + argument);
    }

    public static void WriteButRemoveCommentsAndUsings(string outputPath, Dictionary<string, List<Tuple<FileInfo, string>>> componentGroups)
    {
        foreach (var key in componentGroups.Keys)
        {
            string keyWithoutLessMoreThan = key.Replace('<', 'T').Replace('>', 'T');
            string keyOutPath = Path.Combine(outputPath, componentGroups[key].Count + keyWithoutLessMoreThan);
            Directory.CreateDirectory(keyOutPath);
            List<Tuple<FileInfo, string>> tuples = componentGroups[key];

            Dictionary<string, int> fileNameCount = new Dictionary<string, int>();
            foreach (var tuple in tuples)
            {
                var fileInfo = tuple.Item1;
                string fileText = File.ReadAllText(fileInfo.FullName);
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(fileText);
                var codeWithoutCommentsAndUsings = new CommentsUsingsAndRegionRemover().Visit(syntaxTree.GetRoot().NormalizeWhitespace()).ToString();

                string fullOutPath;
                if (!fileNameCount.ContainsKey(fileInfo.Name))
                {
                    fullOutPath = Path.Combine(keyOutPath, fileInfo.Name);

                    fileNameCount.Add(fileInfo.Name, 1);
                }
                else
                {
                    fullOutPath = Path.Combine(keyOutPath, Path.GetFileNameWithoutExtension(fileInfo.Name) + fileNameCount[fileInfo.Name] + Path.GetExtension(fileInfo.Name));
                    fileNameCount[fileInfo.Name] = fileNameCount[fileInfo.Name] + 1;

                }
                File.WriteAllText(fullOutPath, codeWithoutCommentsAndUsings);
            }

        }
    }

}