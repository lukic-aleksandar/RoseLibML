﻿using RoseLibLS.Transformer;
using RoseLibLS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RoseLibLS.LanguageServer
{
    public class PCFGCommandArguments
    {
        [Required, Range(0.0, 1.0)]
        public double ProbabilityCoefficient { get; set; }
        [Required, DirectoryExists(ErrorMessage = "Input folder doesn't exist.")]
        public string InputFolder { get; set; }
        [Required, DirectoryExists(ErrorMessage = "Output folder doesn't exist.")]
        public string OutputFolder { get; set; }
    }

    public class MCMCCommandArguments
    {
        [Required, DirectoryExists(ErrorMessage = "Input folder doesn't exist.")]
        public string InputFolder { get; set; }
        [Required, FileExists(ErrorMessage = "pCFG file doesn't exist.")]
        public string PCFGFile { get; set; }
        [Required, Range(0.0, int.MaxValue)]
        public int Iterations { get; set; }
        [Required, Range(0.0, int.MaxValue)]
        public int BurnInIterations { get; set; }
        [Required, Range(0.0, 1.0)]
        public double InitialCutProbability { get; set; }
        [Required, Range(0.0, double.MaxValue)]
        public double Alpha { get; set; }
        [Required, Range(0.0, int.MaxValue)]
        public int Threshold { get; set; }
        [Required, DirectoryExists(ErrorMessage = "Output folder doesn't exist.")]
        public string OutputFolder { get; set; }
    }

    public class GetIdiomsCommandArguments
    {
        public string RootNodeType { get; set; }
    }

    public class PreviewCommandArguments
    {
        [Required]
        public string Fragment { get; set; }
        [Required]
        public List<MethodParameter> MethodParameters { get; set; }
    }
}
