using RoseLibLS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibLS.LanguageServer.DTOs.Arguments
{
    public class PCFGCalculationCommandArguments
    {
        [Required, Range(0.0, 1.0)]
        public double ProbabilityCoefficient { get; set; }
        [Required, DirectoryExists(ErrorMessage = "Input folder doesn't exist.")]
        public string InputFolder { get; set; }
        [Required, DirectoryExists(ErrorMessage = "Output folder doesn't exist.")]
        public string OutputFolder { get; set; }
    }
}
