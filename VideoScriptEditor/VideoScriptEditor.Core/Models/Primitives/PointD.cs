/*
    Adapted from https://github.com/dotnet/runtime/blob/master/src/libraries/System.Drawing.Primitives/src/System/Drawing/PointF.cs
    See LICENSE.TXT at https://github.com/dotnet/runtime/blob/master/LICENSE.TXT
*/

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using VideoScriptEditor.Extensions;
using Size = System.Drawing.Size;

namespace VideoScriptEditor.Models.Primitives
{
    /// <summary>
    /// Represents an ordered pair of x and y coordinates that define a point in a two-dimensional plane.
    /// </summary>
    [Serializable]
    public struct PointD : IEquatable<PointD>
    {
        /// <summary>
        /// Creates a new instance of the <see cref='PointD'/> class with member data left uninitialized.
        /// </summary>
        public static readonly PointD Empty;
        private double x; // Do not rename (binary serialization)
        private double y; // Do not rename (binary serialization)

        /// <summary>
        /// Initializes a new instance of the <see cref='PointD'/> class with the specified coordinates.
        /// </summary>
        public PointD(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref='PointD'/> is empty.
        /// </summary>
        [Browsable(false)]
        public readonly bool IsEmpty => x == 0d && y == 0d;

        /// <summary>
        /// Gets the x-coordinate of this <see cref='PointD'/>.
        /// </summary>
        public double X
        {
            readonly get => x;
            set => x = value;
        }

        /// <summary>
        /// Gets the y-coordinate of this <see cref='PointD'/>.
        /// </summary>
        public double Y
        {
            readonly get => y;
            set => y = value;
        }

        /// <summary>
        /// Translates a <see cref='PointD'/> by a given <see cref='System.Drawing.Size'/> .
        /// </summary>
        public static PointD operator +(PointD pt, Size sz) => Add(pt, sz);

        /// <summary>
        /// Translates a <see cref='PointD'/> by the negative of a given <see cref='System.Drawing.Size'/> .
        /// </summary>
        public static PointD operator -(PointD pt, Size sz) => Subtract(pt, sz);

        /// <summary>
        /// Translates a <see cref='PointD'/> by a given <see cref='SizeD'/> .
        /// </summary>
        public static PointD operator +(PointD pt, SizeD sz) => Add(pt, sz);

        /// <summary>
        /// Translates a <see cref='PointD'/> by the negative of a given <see cref='SizeD'/> .
        /// </summary>
        public static PointD operator -(PointD pt, SizeD sz) => Subtract(pt, sz);

        /// <summary>
        /// Compares two <see cref='PointD'/> objects. The result specifies whether the values of the
        /// <see cref='PointD.X'/> and <see cref='PointD.Y'/> properties of the two
        /// <see cref='PointD'/> objects are equal.
        /// </summary>
        public static bool operator ==(PointD left, PointD right) => left.X == right.X && left.Y == right.Y;

        /// <summary>
        /// Compares two <see cref='PointD'/> objects. The result specifies whether the values of the
        /// <see cref='PointD.X'/> or <see cref='PointD.Y'/> properties of the two
        /// <see cref='PointD'/> objects are unequal.
        /// </summary>
        public static bool operator !=(PointD left, PointD right) => !(left == right);

        /// <summary>
        /// Translates a <see cref='PointD'/> by a given <see cref='System.Drawing.Size'/> .
        /// </summary>
        public static PointD Add(PointD pt, Size sz) => new PointD(pt.X + sz.Width, pt.Y + sz.Height);

        /// <summary>
        /// Translates a <see cref='PointD'/> by the negative of a given <see cref='System.Drawing.Size'/> .
        /// </summary>
        public static PointD Subtract(PointD pt, Size sz) => new PointD(pt.X - sz.Width, pt.Y - sz.Height);

        /// <summary>
        /// Translates a <see cref='PointD'/> by a given <see cref='SizeD'/> .
        /// </summary>
        public static PointD Add(PointD pt, SizeD sz) => new PointD(pt.X + sz.Width, pt.Y + sz.Height);

        /// <summary>
        /// Translates a <see cref='PointD'/> by the negative of a given <see cref='SizeD'/> .
        /// </summary>
        public static PointD Subtract(PointD pt, SizeD sz) => new PointD(pt.X - sz.Width, pt.Y - sz.Height);

        /// <inheritdoc/>
        public override readonly bool Equals(object obj) => obj is PointD pt && Equals(pt);

        /// <inheritdoc/>
        public readonly bool Equals(PointD other) => this == other;

        /// <inheritdoc/>
        public override readonly int GetHashCode() => HashCode.Combine(X.GetHashCode(), Y.GetHashCode());

        /// <summary>
        /// Creates a human-readable string that represents this <see cref='PointD'/>.
        /// </summary>
        public override readonly string ToString() => "{X=" + x.ToString() + ", Y=" + y.ToString() + "}";

        /// <summary>
        /// Linearly interpolates between two <see cref="PointD">points</see> based on the given weighting.
        /// </summary>
        /// <param name="value1">The first source <see cref="PointD">point</see>.</param>
        /// <param name="value2">The second source <see cref="PointD">point</see>.</param>
        /// <param name="amount">Value indicating the weight of the second source <see cref="PointD">point</see>.</param>
        /// <returns>The interpolated <see cref="PointD">point</see>.</returns>
        public static PointD Lerp(PointD value1, PointD value2, double amount)
        {
            return new PointD(
                value1.X.LerpTo(value2.X, amount),
                value1.Y.LerpTo(value2.Y, amount)
            );
        }
    }
}
