using NUnit.Framework;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using MersenneTwister;
using RoseLibML;
using RoseLibML.Core;

namespace Tests.AlgorithmTests.MathTests
{
    class LnPrecisionTests
    {

        [Test]
        public void TestDiffMultiplication()
        {
            for (int i = 0; i < 50; i++)
            {
                var x = Randoms.WellBalanced.NextDouble() * 1000;
                var y = Randoms.WellBalanced.NextDouble() * 1000;

                var resultBasic = x * y;
                var resultUsingLn = Math.Exp(Math.Log(x) + Math.Log(y));

                var roundedBasic = Math.Round(resultBasic, 5);
                var roundedUsingLn = Math.Round(resultUsingLn, 5);
                Assert.AreEqual(roundedBasic, roundedUsingLn);
            }
        }

        [Test]
        public void TestDiffDivision()
        {
            for (int i = 0; i < 50; i++)
            {
                var x = Randoms.WellBalanced.NextDouble() * 1000;
                var y = Randoms.WellBalanced.NextDouble() * 1000;

                var resultBasic = x / y;
                var resultUsingLn = Math.Exp(Math.Log(x) - Math.Log(y));

                var roundedBasic = Math.Round(resultBasic, 5);
                var roundedUsingLn = Math.Round(resultUsingLn, 5);
                Assert.AreEqual(roundedBasic, roundedUsingLn);
            }
        }

        // v * w * x
        //-----------
        //  y * z
        [Test]
        public void TestDiffCombination()
        {
            for (int i = 0; i < 5000; i++)
            {
                var v = Randoms.WellBalanced.NextDouble() * 1000;
                var w = Randoms.WellBalanced.NextDouble() * 1000;
                var x = Randoms.WellBalanced.NextDouble() * 1000;
                var y = Randoms.WellBalanced.NextDouble() * 1000;
                var z = Randoms.WellBalanced.NextDouble() * 1000;

                var resultBasic = v * w * x / (y * z);
                var resultUsingLn = Math.Exp(Math.Log(v) + Math.Log(w) + Math.Log(x) - (Math.Log(y) + Math.Log(z)));

                var roundedBasic = Math.Round(resultBasic, 4);
                var roundedUsingLn = Math.Round(resultUsingLn, 4);
                Assert.AreEqual(roundedBasic, roundedUsingLn);
            }
        }
    }
}
