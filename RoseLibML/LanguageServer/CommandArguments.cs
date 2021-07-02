namespace RoseLibML.LanguageServer
{
    public class PCFGCommandArguments
    {
        public double ProbabilityCoefficient { get; set; } = 0.0001;
        public string InputFolder { get; set; }
        public string OutputFile { get; set; }
    }

    public class MCMCCommandArguments
    {
        public string InputFolder { get; set; }
        public string PCFGFile { get; set; }
        public int Iterations { get; set; }
        public int BurnInIterations { get; set; }
        public double InitialCutProbability { get; set; } = 0.8;
        public double Alpha { get; set; } = 2;
        public int Threshold { get; set; }
        public string OutputFolder { get; set; }
    }
}
