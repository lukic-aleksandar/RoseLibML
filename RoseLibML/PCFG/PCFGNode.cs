using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLib
{
    [Serializable]
    public class PCFGNode
    {
        public int Count { get; set; }
        public double Probability { get; set; }
        public string RHS { get; set; }

        public PCFGNode()
        {
        }

        public PCFGNode(string rhs)
        {
            RHS = rhs;
        }

        public void Increment()
        {
            Count += 1;
        }
    }
}
