using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.Core.PCFG
{
    [Serializable]
    public class PCFGRHSData
    {
        public int Count { get; set; }
        public double ProbabilityLn { get; set; }
        public string RHS { get; set; }

        public PCFGRHSData()
        {
        }

        public PCFGRHSData(string rhs)
        {
            RHS = rhs;
        }

        public void Increment()
        {
            Count += 1;
        }
    }
}
