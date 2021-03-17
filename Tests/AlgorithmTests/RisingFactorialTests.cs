using NUnit.Framework;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using MersenneTwister;
using RoseLibML;

namespace Tests.AlgorithmTests
{
    class RisingFactorialTests
    {
        private double RisingFactorialReal(double x, double n)
        {
            return SpecialFunctions.Gamma(x + n) / SpecialFunctions.Gamma(x);
        }



        [Test]
        public void TestDiffBasicVSOptimized()
        {
            for(int i = 0; i < 1000; i++)
            {
                var x = Randoms.WellBalanced.Next(10000);
                var n = Randoms.WellBalanced.Next(10000);
                var resultBasic = RisingFactorialBI(x, n);
                var resultOptimized = TBSampler.RisingFactorialBIOptimized(x, 0, n-1);

                Assert.AreEqual(resultBasic, resultOptimized);
            }
        }

        public BigInteger RisingFactorialBI(int x, int n)
        {
            BigInteger result = new BigInteger(1);
            for (int k = 0; k < n; k++)
            {
                result = result * new BigInteger(x + k);
            }

            return result;
        }

        [Test]
        public void TestMaxGamma()
        {
            var parameter = 1;
            while(SpecialFunctions.Gamma(parameter) < Math.Pow(2,53))
            {
                parameter++;
            }

            Assert.Pass();
        }
        
    }
}
