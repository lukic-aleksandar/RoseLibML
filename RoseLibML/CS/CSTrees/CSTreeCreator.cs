using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoseLibML.Core.LabeledTrees;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.CS.CSTrees
{
    public class CSTreeCreator: TreeCreator
    {
        public static CSTree CreateTree(FileInfo sourceInfo, string outDirectory)
        {
            using (StreamReader sr = new StreamReader(sourceInfo.FullName))
            {
                var source = sr.ReadToEnd();
                var syntaxTree = CSharpSyntaxTree.ParseText(source);

                var tree = new CSTree();

                tree.SourceFilePath = sourceInfo.FullName;
                if (outDirectory != null)
                {
                    tree.FilePath = $"{outDirectory}/{sourceInfo.Name}.bin";
                }

                tree.Root = CSNodeCreator.CreateNode(syntaxTree.GetRoot());
                tree.Root.IsFragmentRoot = true;
                tree.Root.CanHaveType = false;

                return tree;
            }
        }

        
    }
}
