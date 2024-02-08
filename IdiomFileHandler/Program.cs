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
                    Console.WriteLine("Not implemented yet..");
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
                Console.Write("(Adjacent) Idiom marks separated by a comma : "); idiomMarksCSV = Console.ReadLine();

                Console.WriteLine($"The input was: {path}, {idiomMarksCSV}");
            } while (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(idiomMarksCSV));

            IdiomJoiner.JoinIdioms(path, idiomMarksCSV);
            
        }
    }
}