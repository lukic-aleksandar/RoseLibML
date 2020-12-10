using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML
{
    public class LabeledTree
    {
        public LabeledNode Root { get; set; }
        public string SourceFilePath { get; set; }
        public string FilePath { get; set; }

        public void Serialize()
        {
            Root.Serialize(FilePath);
        }
    }
}
