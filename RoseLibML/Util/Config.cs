using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.Util
{
    public class Config
    {
        public ModelParams? ModelParams { get; set; }
        public Paths? Paths { get; set; }
        public RunParams? RunParams { get; set; }
    }

    public class ModelParams
    {
        [Range(0.0, double.MaxValue)]
        public double Alpha { get; set; }
        [Range(0.0, 1.0)]
        public double CutProbability { get; set; }
        [Range(0.0, 1.0)]
        public double P { get; set; }
    }

    public class Paths
    {
        [Required(AllowEmptyStrings = false)]
        public string? InData { get; set; }
        public string? InModel { get; set; }
        public string? OutModel { get; set; }
        [Required(AllowEmptyStrings = false)]
        public string? OutIdioms { get; set; }
    }

    public class RunParams
    {
        [Range(0.0, int.MaxValue)]
        public int StartIteration { get; set; }
        [Range(0.0, int.MaxValue)]
        public int BurnIn { get; set; }
        [Range(0.0, int.MaxValue)]
        public int TotalIterations { get; set; }
        [Range(0.0, int.MaxValue)]
        public int Threshold { get; set; }
        public bool JustWriteTheFragments { get; set; }
    }
}
