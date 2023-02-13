using NUnit.Framework;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using MersenneTwister;
using RoseLibML;
using RoseLibML.Core;

namespace RoseLibMLTests.AlgorithmTests.MathTests
{
    class CombinationsTests
    {
        [Test]
        public void TestDiffBasicVSBigNumbers()
        {
            var resultBasic = BasicCombinationsWithoutRepetition(5, 3);
            var resultOptimized = MathFunctions.CombinationsWithoutRepetition(5, 3);

            // Working with big numbers means we approximate division
            // The difference should be really small
            Assert.LessOrEqual(Math.Abs(resultBasic - resultOptimized), 0.1);
        }

        public double BasicCombinationsWithoutRepetition(int n, int k)
        {
            var numerator = SpecialFunctions.Factorial(n);
            var denominator = SpecialFunctions.Factorial(n - k) * SpecialFunctions.Factorial(k);
            return numerator / (double)denominator;
        }
    }
}
