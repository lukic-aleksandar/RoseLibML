using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoseLibML;
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
            //ApplicationCommands.cs

            var sampler = new GibbsSampler();
            sampler.Initialize(@"C:\Users\93luk\Desktop\RoseLibMLTraining\training1000", @"C:\Users\93luk\Desktop\RoseLibMLTraining\output1000");
            sampler.Train(3);

            foreach (var tree in sampler.Trees)
            {
                tree.Serialize();
            }

            Console.ReadKey();
        }
    }
}
