using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLib
{
    class Program
    {
        static void Main(string[] args)
        {
            var pcfg = new PCFGComposer("PCFGComposer.cs");
            pcfg.CalculateProbabilities();

            using (var sr = new StreamReader("TestFile.cs"))
            {
                var root = CSharpSyntaxTree.ParseText(sr.ReadToEnd()).GetRoot()
                                    .DescendantNodes().OfType<ClassDeclarationSyntax>().First();
                var proba = pcfg.CalculateFragmentProbability(root);
            }
          

            // The code provided will print ‘Hello World’ to the console.
            // Press Ctrl+F5 (or go to Debug > Start Without Debugging) to run your app.
            Console.WriteLine("Hello World!");
            Console.ReadKey();
            // Go to http://aka.ms/dotnet-get-started-console to continue learning how to build a console app! 
        }
    }
}
