/* Inspired by sample code written by Rob Blackbourn on his blog at http://geekswithblogs.net/blackrob/archive/2014/11/14/a-rational-number-class-in-c.aspx */

using System;
using VideoScriptEditor.Extensions;

namespace VideoScriptEditor.Models.Primitives
{
    /// <summary>
    /// Represents a rational fraction with a numerator and denominator.
    /// </summary>
    [Serializable]
    public struct Ratio : IEquatable<Ratio>
    {
        /// <summary>
        /// The numerator. A number to be divided by the <see cref="Denominator"/>.
        /// </summary>
        public uint Numerator;

        /// <summary>
        /// The denominator. A number that functions as the divisor of the <see cref="Numerator"/>.
        /// </summary>
        public uint Denominator;

        /// <summary>
        /// Gets a <see cref="Ratio"/> representing one to one.
        /// </summary>
        public static Ratio OneToOne => new Ratio(1, 1);

        /// <summary>
        /// Creates a new <see cref="Ratio"/>.
        /// </summary>
        /// <param name="numerator">The numerator of the <see cref="Ratio"/>.</param>
        /// <param name="denominator">The denominator of the <see cref="Ratio"/>. Cannot be zero.</param>
        public Ratio(uint numerator, uint denominator) : this(numerator, denominator, false)
        {
        }

        /// <summary>
        /// Creates a new <see cref="Ratio"/>, optionally reducing to its simplest form.
        /// </summary>
        /// <param name="numerator">The numerator of the <see cref="Ratio"/>.</param>
        /// <param name="denominator">The denominator of the <see cref="Ratio"/>. Cannot be zero.</param>
        /// <param name="simplify">Whether to reduce the <see cref="Ratio"/> to its simplest form.</param>
        public Ratio(uint numerator, uint denominator, bool simplify)
        {
            if (denominator == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(denominator), "The denominator cannot be 0.");
            }

            Numerator = numerator;
            Denominator = denominator;

            if (simplify)
            {
                Simplify();
            }
        }

        /// <summary>
        /// Reduces the <see cref="Ratio"/> to its simplest form.
        /// </summary>
        public void Simplify()
        {
            if (Denominator != 1)
            {
                if (Numerator == Denominator)
                {
                    Numerator = 1;
                    Denominator = 1;
                }
                else
                {
                    uint gcd = MathExtensions.GCD(Numerator, Denominator);
                    Numerator /= gcd;
                    Denominator /= gcd;
                }
            }
        }

        /// <summary>
        /// Creates a human-readable string that represents this <see cref='Ratio'/>.
        /// </summary>
        public override string ToString()
        {
            return Numerator + ":" + Denominator;
        }

        /// <inheritdoc/>
        public bool Equals(Ratio other)
        {
            return Numerator == other.Numerator && Denominator == other.Denominator;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is Ratio otherRatio && Equals(otherRatio);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Numerator, Denominator);
        }

        /// <summary>
        /// Compares two <see cref="Ratio"/> instances for exact equality.
        /// </summary>
        /// <param name="left">The left hand side <see cref="Ratio"/> instance to compare.</param>
        /// <param name="right">The right hand side <see cref="Ratio"/> instance to compare.</param>
        /// <returns>True if the two <see cref="Ratio"/> instances are exactly equal, False otherwise.</returns>
        public static bool operator ==(Ratio left, Ratio right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="Ratio"/> instances for exact inequality.
        /// </summary>
        /// <param name="left">The left hand side <see cref="Ratio"/> instance to compare.</param>
        /// <param name="right">The right hand side <see cref="Ratio"/> instance to compare.</param>
        /// <returns>True if the two <see cref="Ratio"/> instances are exactly unequal, False otherwise.</returns>
        public static bool operator !=(Ratio left, Ratio right)
        {
            return !(left == right);
        }
    }
}
