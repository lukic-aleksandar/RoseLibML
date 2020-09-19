using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.LanguageServer
{
    public class PCFGCommandArguments
    {
        public double ProbabilityCoefficient { get; set; } = 0.0001;
        public string InputFolder { get; set; }
        public string OutputFile { get; set; }
    }
}
