using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLib
{
    public class PCFGNode
    {
        public int Count { get; set; }
        public double Probability { get; set; }
        public IEnumerable<SyntaxNodeOrToken> Nodes { get; set; }
        public string RHS { get; set; }

        public PCFGNode()
        {
            Nodes = new List<SyntaxNodeOrToken>();
        }

        public PCFGNode(IEnumerable<SyntaxNodeOrToken> nodes, string rhs)
        {
            Nodes = nodes;
            RHS = rhs;
        }

        public void Increment()
        {
            Count += 1;
        }
    }
}
