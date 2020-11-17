using System;

namespace VideoScriptEditor.Extensions
{
    /// <summary>
    /// Extension methods and helpers for mathematical functions.
    /// </summary>
    public static class MathExtensions
    {
        /// <summary>
        /// The number of fractional digits to use in rounding operations.
        /// </summary>
        public const int FloatingPointPrecision = 6;

        /// <summary>
        /// Linearly interpolates between two <see cref="double"/> values based on the given weighting.
        /// </summary>
        /// <param name="self">The first source value.</param>
        /// <param name="other">The second source value.</param>
        /// <param name="amount">Value indicating the weight of the second source value.</param>
        /// <returns>The interpolated <see cref="double"/> value.</returns>
        public static double LerpTo(this double self, double other, double amount)
        {
            if (amount == 0d)
            {
                return self;
            }
            else if (amount == 1d)
            {
                return other;
            }
            else
            {
                return self + ((other - self) * amount);
            }
        }

        /// <summary>
        /// An implementation of the Euclidean algorithm that returns the greatest common divisor without performing any heap allocation.
        /// </summary>
        /// <remarks>
        /// Based on sample code posted by Drew Noakes on Stack Overflow at https://stackoverflow.com/questions/18541832/c-sharp-find-the-greatest-common-divisor/41766138#41766138
        /// </remarks>
        /// <returns>The greatest common divisor of <paramref name="a"/> and <paramref name="b"/>.</returns>
        public static uint GCD(uint a, uint b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            return a == 0 ? b : a;
        }

        /// <summary>
        /// Rounds a double-precision floating-point value to the nearest even integral number.
        /// </summary>
        /// <remarks>
        /// Based on sample code posted by Adam Wright on Stack Overflow at https://stackoverflow.com/questions/4360348/how-to-find-nearest-even-number-for-given-int-given-11-return-12/22894257#22894257
        /// </remarks>
        /// <param name="value">A double-precision floating-point number to be rounded.</param>
        /// <returns>The even integral number nearest to <paramref name="value"/>.</returns>
        public static int RoundToNearestEvenIntegral(this double value)
        {
            return (int)Math.Round(value * 0.5) * 2;
        }

        /// <summary>
        /// Rounds an integral number to the nearest even integral number.
        /// </summary>
        /// <param name="value">An integral number to be rounded.</param>
        /// <returns>The even integral number nearest to <paramref name="value"/>.</returns>
        public static int RoundToNearestEvenIntegral(this int value)
        {
            return RoundToNearestEvenIntegral((double)value);
        }
    }
}
