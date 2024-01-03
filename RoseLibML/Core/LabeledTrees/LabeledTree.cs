using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.Core.LabeledTrees
{
    public class LabeledTree
    {
        public LabeledNode Root { get; set; }
        public string SourceFilePath { get; set; }
        public string FilePath { get; set; }

        #region For HTML visualization
        // Without an extension
        public string FileName { get; set; }

        #endregion
        public void Serialize()
        {
            Root.Serialize(FilePath);
        }
    }
}
