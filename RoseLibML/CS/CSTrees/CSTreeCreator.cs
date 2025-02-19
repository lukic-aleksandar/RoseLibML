﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.CS.CSTrees
{
    public class CSTreeCreator : TreeCreator
    {
        public static CSTree CreateTree(FileInfo sourceInfo, string outDirectory, FixedNodeKinds? fixedNodeKinds = null)
        {
            using (StreamReader sr = new StreamReader(sourceInfo.FullName))
            {
                CSNodeCreator csNodeCreator = new CSNodeCreator(fixedNodeKinds); 

                var source = sr.ReadToEnd();
                var syntaxTree = CSharpSyntaxTree.ParseText(source);

                var tree = new CSTree();

                tree.SourceFilePath = sourceInfo.FullName;
                if (outDirectory != null)
                {
                    tree.FilePath = $"{outDirectory}/{sourceInfo.Name}.bin";
                }

                try 
                {
                    tree.Root = csNodeCreator.CreateNode(syntaxTree.GetRoot());
                    tree.Root.IsFragmentRoot = true;
                    tree.Root.CanHaveType = false;

                    return tree;
                }
                catch 
                {
                    Console.WriteLine($"WAS NOT ABLE TO CREATE A LARGE TREE at: {sourceInfo.FullName}");
                    return null;
                }

            }
        }

        public static CSTree CreateTree(string sourceCode, FixedNodeKinds? fixedNodeKinds = null)
        {
            CSNodeCreator csNodeCreator = new CSNodeCreator(fixedNodeKinds);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

            var tree = new CSTree();

            tree.Root = csNodeCreator.CreateNode(syntaxTree.GetRoot());
            tree.Root.IsFragmentRoot = true;
            tree.Root.CanHaveType = false;

            return tree;
        }
        private static string Extension { get; set; } = ".bin";
        // in and out model paths are directories
        public static CSTree Deserialize(FileInfo sourceInfo, string inModelPath, string outModelPath) 
        {
            var inTreeFullPath = Path.Combine(inModelPath, sourceInfo.Name + Extension);
            bool inTreeExists = File.Exists(inTreeFullPath);

            if (inTreeExists)
            {
                var tree = new CSTree();
                tree.Root = CSNode.Deserialize(inTreeFullPath);
                tree.SourceFilePath = sourceInfo.FullName;
                if (outModelPath != null)
                {
                    tree.FilePath = $"{outModelPath}/{sourceInfo.Name}{Extension}";
                }
                return tree;
            }

            return null;
        }

        public static CSTree Deserialize(string csSource, string binSource, string fileName)
        {
            bool inTreeExists = File.Exists(binSource);
            if (inTreeExists)
            {
                var tree = new CSTree();
                tree.Root = CSNode.Deserialize(binSource);
                tree.SourceFilePath = csSource;
                tree.FilePath = binSource;
                tree.FileName = fileName;
                return tree;
            }

            return null;
        }
    }
}
