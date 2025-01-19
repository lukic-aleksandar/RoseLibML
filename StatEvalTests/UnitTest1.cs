using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using RoseLibML.Core.LabeledTrees;
using RoseLibML.CS.CSTrees;
using RoseLibML.Util;
using StatEval;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.RegularExpressions;

namespace StatEvalTests
{
    public class Tests
    {
        [Test]
        public void TestTBNDeserialization()
        {
            //var idiomInTBN = "(8842(namespace) (8616 (StatEvalDemo)  ) ({) (8855) (})  )";
            var idiomInTBN = "(8875 (public) (static) (8621 (void)  ) (B_8875 (IdentifierToken (ReadTrainingFiles)  ) (B_8875 (8906 (() (8908 (8621 (string)  ) (IdentifierToken (inDataDir)  )  ) (,) (8908 (8621 (string)  ) (IdentifierToken (outModelDir)  )  ) ())  ) (8792 ({) (8797 (8634 (8616 (LoadCounterpartsPaths)  ) (8636 (() (8638 (8616 (inDataDir)  )  ) (,) (8638 (8616 (outModelDir)  )  ) ())  )  ) (;)  ) (8797 (8634 (8616 (LoadCounterpartsTrees)  ) (8636 (() ())  )  ) (;)  ) (})  )  )  )  ) ";


            var sanitizedIdiom = idiomInTBN
                .Trim()
                .Replace("(()", "(%op%)")
                .Replace("())", "(%cp%)");

            var level = 0;
            var currentWord = "";

            var idiomReadyForDeserialization = sanitizedIdiom
                .Substring(1, sanitizedIdiom.Length - 2)
                .Trim();

            var currentNode = new CSNode();

            foreach(var ch in idiomReadyForDeserialization)
            {
                if(ch == '(')
                {
                    level++;
                    if (currentNode.STInfo == null || currentNode.STInfo.Length == 0)
                    {
                        currentNode.STInfo = currentWord.Trim();
                        if (currentNode.STInfo == "%op%") currentNode.STInfo = "(";
                        if (currentNode.STInfo == "%cp%") currentNode.STInfo = ")";
                    }
                    currentWord = "";
                    
                    var newChildNode = new CSNode();

                    newChildNode.Parent = currentNode;
                    currentNode.Children.Add(newChildNode);

                    currentNode = newChildNode;
                    continue;
                }
                if(ch == ')')
                {
                    level--;

                    if (currentNode.STInfo == null || currentNode.STInfo.Length == 0)
                    {
                        currentNode.STInfo = currentWord.Trim();
                        if (currentNode.STInfo == "%op%") currentNode.STInfo = "(";
                        if (currentNode.STInfo == "%cp%") currentNode.STInfo = ")";
                    }
                    currentWord = "";

                    currentNode = (CSNode) currentNode.Parent;
                    continue;
                }

                currentWord += ch;
            }

            if(level != 0)
            {
                Assert.Fail();
            }

            Assert.Pass();
        }
    }
}