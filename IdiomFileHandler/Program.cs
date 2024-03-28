using IdiomFileHandler;

namespace IdiomFileGrouper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            char? chosenOption = null;
            do
            {
                Console.WriteLine();
                Console.WriteLine("Please choose one of the available options:");
                Console.WriteLine("1. Join different idioms to one"); // They really should be adjasent, who knows what happens if they are not...
                Console.WriteLine("2. Group files that contain a specific idiom");
                Console.WriteLine("");
                Console.WriteLine("Press x to exit the application");
                Console.WriteLine();

                chosenOption = Console.ReadKey().KeyChar;
                Console.WriteLine();

                ProcessCommand(command: (char)chosenOption);
            } while (chosenOption != 'x');
        }

        static void ProcessCommand(char command) 
        { 
            switch (command)
            {
                case '1':
                    ProcessIdiomJoining();
                    break;
                case '2':
                    ProcessIdiomFileGrouping();
                    break;
                default:
                    return;
            }
        }

        private static void ProcessIdiomJoining()
        {
            var path = "";
            var idiomMarksCSV = "";

            do
            {
                Console.WriteLine();
                Console.WriteLine("To join different idioms together, please provide the following:");
                Console.Write("Binary files path: "); path = Console.ReadLine();
                Console.Write("Two (adjacent) idiom marks separated by a comma : "); idiomMarksCSV = Console.ReadLine();

                Console.WriteLine($"The input was: {path}, {idiomMarksCSV}");
            } while (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(idiomMarksCSV));

            IdiomJoiner.JoinIdioms(path, idiomMarksCSV);
            
        }

        private static void ProcessIdiomFileGrouping()
        {
            var binPath = "";
            var csPath = "";
            var idiomMark = "";
            var outPath = "";

            do
            {
                Console.WriteLine();
                Console.WriteLine("To group files based on an idiom, please provide the following:");
                Console.Write("Binary files path: "); binPath = Console.ReadLine();
                Console.Write("CS files path: "); csPath = Console.ReadLine();
                Console.Write("Idiom's mark: "); idiomMark = Console.ReadLine();
                Console.Write("Output files path: "); outPath = Console.ReadLine();

            } while (
                string.IsNullOrEmpty(binPath) || 
                string.IsNullOrEmpty(csPath) ||
                string.IsNullOrEmpty(idiomMark) ||
                string.IsNullOrEmpty(outPath)
                );

            var group = IdiomFileGrouper.GroupFilesBasedOnIdiomMark(binPath, idiomMark);
            if (group != null && group.Count > 0)
            {
                var allCSFiles = Directory.GetFiles(csPath);

                foreach( var fileInfo in group )
                {
                    var csFileRootInName = fileInfo.Name.Substring(0, fileInfo.Name.Length - 4);
                    foreach( var csFile in allCSFiles )
                    {
                        var csFileInfo = new FileInfo(csFile);
                        if(csFileInfo.Name == csFileRootInName)
                        {
                            File.Copy(csFileInfo.FullName, Path.Combine(outPath, csFileInfo.Name));
                        }
                    }
                }
            }
        }
    }
}