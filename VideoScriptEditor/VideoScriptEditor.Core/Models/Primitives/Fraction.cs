/* Inspired by sample code written by Rob Blackbourn on his blog at http://geekswithblogs.net/blackrob/archive/2014/11/14/a-rational-number-class-in-c.aspx */

using System;

namespace VideoScriptEditor.Models.Primitives
{
    /// <summary>
    /// Represents a fractional number with a numerator and denominator.
    /// </summary>
    [Serializable]
    public struct Fraction : IEquatable<Fraction>
    {
        /// <summary>
        /// The numerator. A number to be divided by the <see cref="Denominator"/>.
        /// </summary>
        public int Numerator;

        /// <summary>
        /// The denominator. A number that functions as the divisor of the <see cref="Numerator"/>.
        /// </summary>
        public int Denominator;

        /// <summary>
        /// Gets the floating-point equivalent of the current <see cref="Fraction"/>.
        /// </summary>
        public double PrecisionValue => (double)Numerator / Denominator;

        /// <summary>
        /// Gets the inverse floating-point equivalent of the current <see cref="Fraction"/>.
        /// </summary>
        public double InvertedPrecisionValue => (double)Denominator / Numerator;

        /// <summary>
        /// Creates a new <see cref="Fraction"/>.
        /// </summary>
        /// <param name="numerator">The numerator of the <see cref="Fraction"/>.</param>
        /// <param name="denominator">The denominator of the <see cref="Fraction"/>. Cannot be zero.</param>
        public Fraction(int numerator, int denominator)
        {
            if (denominator == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(denominator), "The denominator cannot be 0.");
            }

            Numerator = numerator;
            Denominator = denominator;
        }

        /// <summary>
        /// Creates a human-readable string that represents this <see cref="Fraction"/>.
        /// </summary>
        public override string ToString()
        {
            return Numerator + "/" + Denominator;
        }

        /// <inheritdoc/>
        public bool Equals(Fraction other)
        {
            return Numerator == other.Numerator && Denominator == other.Denominator;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Numerator, Denominator);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is Fraction otherFraction && Equals(otherFraction);
        }

        /// <summary>
        /// Compares two <see cref="Fraction"/> instances for exact equality.
        /// </summary>
        /// <param name="left">The left hand side <see cref="Fraction"/> instance to compare.</param>
        /// <param name="right">The right hand side <see cref="Fraction"/> instance to compare.</param>
        /// <returns>True if the two <see cref="Fraction"/> instances are exactly equal, False otherwise.</returns>
        public static bool operator ==(Fraction left, Fraction right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="Fraction"/> instances for exact inequality.
        /// </summary>
        /// <param name="left">The left hand side <see cref="Fraction"/> instance to compare.</param>
        /// <param name="right">The right hand side <see cref="Fraction"/> instance to compare.</param>
        /// <returns>True if the two <see cref="Fraction"/> instances are exactly unequal, False otherwise.</returns>
        public static bool operator !=(Fraction left, Fraction right)
        {
            return !(left == right);
        }
    }
}
