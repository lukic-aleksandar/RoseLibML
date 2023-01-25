using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML.Core
{
    public static class MathFunctions
    {
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double RisingFactorial(double x, double n)
        {
            if (n == 0) return 1;
            if(x == 0) return 0;

            var lnResult = RisingFactorialLn(x, n);
            return Math.Exp(lnResult);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double RisingFactorialLn(double x, double n)
        {
            if (n == 0) return 1;
            if (x == 0) return 0;

            var leftSubtractionValue = SpecialFunctions.GammaLn(x + n);
            var rightSubtractionValue = SpecialFunctions.GammaLn(x);


            var lnResult = leftSubtractionValue - rightSubtractionValue;
            return lnResult;
        }

        /// <summary>
        /// Combinations without repetitions aware of large numbers.
        /// </summary>
        /// <param name="n">Total number of elements</param>
        /// <param name="k">Elements to be taken, regardless of the order</param>
        /// <returns>Result, if possible</returns>
        public static double CombinationsWithoutRepetition(int n, int k)
        {
            if (n <= 0)
            {
                throw new ArgumentOutOfRangeException("n", "Value must be positive (and not zero).");
            }
            if (k < 0)
            {
                throw new ArgumentOutOfRangeException("k", "Value must be positive.");
            }
            if (n < k)
            {
                throw new ArgumentOutOfRangeException("k", "Value must be larger than n.");
            }

            if (n <= 22)
            {
                var numerator = SpecialFunctions.Factorial(n);
                var denominator = SpecialFunctions.Factorial(n - k) * SpecialFunctions.Factorial(k);
                return numerator / (double)denominator;
            }
            else
            {
                var numeratorLn = SpecialFunctions.FactorialLn(n);
                var denominatorLn = SpecialFunctions.FactorialLn(n - k) + SpecialFunctions.FactorialLn(k);
                var resultLn = numeratorLn - denominatorLn;

                return Math.Exp(resultLn);
            }
        }
    }
}
