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
                var x = (uint) Randoms.WellBalanced.Next(10000);
                var n = (uint)Randoms.WellBalanced.Next(10000);
                var resultBasic = RisingFactorialBI(x, n);
                var resultOptimized = TBSampler.RisingFactorialBIOptimized(x, 1, n);

                Assert.AreEqual(resultBasic, resultOptimized);
            }
        }

        [Test]
        public void TestDiffBasicVSOptimizedx0()
        {
            uint x = 0;
            uint n = 1;
            var resultBasic = RisingFactorialBI(x, n);
            var resultOptimized = TBSampler.RisingFactorialBIOptimized(x, 1, n);

            Assert.AreEqual(resultBasic, resultOptimized);
        }

        [Test]
        public void TestDiffBasicVSOptimizedn0()
        {
            uint x = 2;
            uint n = 0;
            var resultBasic = RisingFactorialBI(x, n);
            var resultOptimized = TBSampler.RisingFactorialBIOptimized(x, n, n);

            Assert.AreEqual(resultBasic, resultOptimized);
        }

        public BigInteger RisingFactorialBI(uint x, uint n)
        {
            if(n == 0)
            {
                return 1;
            }
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
