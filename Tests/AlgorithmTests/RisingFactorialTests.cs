using NUnit.Framework;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using MersenneTwister;
using RoseLibML;
using RoseLibML.Core;

namespace Tests.AlgorithmTests
{
    class RisingFactorialTests
    {
        private double RisingFactorialReal(double x, double n)
        {
            return SpecialFunctions.Gamma(x + n) / SpecialFunctions.Gamma(x);
        }

        [Test]
        public void TestDiffGammaLnVSBasic()
        {
            for (int i = 0; i < 40; i++)
            {
                var x = Randoms.WellBalanced.NextDouble() * 10;
                var n = Randoms.WellBalanced.NextDouble() * 10;

                var resultBasic = RisingFactorialReal(x, n);
                var resultLn = MathFunctions.RisingFactorial(x, n);

                var roundedBasic = Math.Round(resultBasic);
                var roundedLn = Math.Round(resultLn);
                Assert.AreEqual(roundedBasic, roundedLn);
            }
        }



        [Test]
        public void TestRisingFactorialOf0()
        {
            uint x = 0;
            uint n = 1;
            var result = MathFunctions.RisingFactorial(x, n);

            Assert.AreEqual(0, result);
        }

        [Test]
        public void TestRisingFactorialRaisedBy0()
        {
            uint x = 1;
            uint n = 0;
            var result = MathFunctions.RisingFactorial(x, n);

            Assert.AreEqual(1, result);
        }
    }
}
