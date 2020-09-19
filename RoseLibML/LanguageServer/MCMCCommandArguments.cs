using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.LanguageServer
{
    public class MCMCCommandArguments
    {
        public string InputFolder { get; set; }
        public string PCFGFile { get; set; }
        public int Iterations { get; set; }
        public int BurnInIterations { get; set; }
        public double InitialCutProbability { get; set; } = 0.8;
        public double Alpha { get; set; } = 2;
        public string OutputFolder { get; set; }
    }
}
