/*
    Adapted from https://github.com/dotnet/runtime/blob/master/src/libraries/System.Drawing.Primitives/src/System/Drawing/SizeF.cs
    See LICENSE.TXT at https://github.com/dotnet/runtime/blob/master/LICENSE.TXT
*/

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;

namespace VideoScriptEditor.Models.Primitives
{
    /// <summary>
    /// Represents the size of a rectangular region with an ordered pair of width and height.
    /// </summary>
    [Serializable]
    public struct SizeD : IEquatable<SizeD>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='SizeD'/> class.
        /// </summary>
        public static readonly SizeD Empty;
        private double width; // Do not rename (binary serialization)
        private double height; // Do not rename (binary serialization)

        /// <summary>
        /// Initializes a new instance of the <see cref='SizeD'/> class from the specified
        /// existing <see cref='SizeD'/>.
        /// </summary>
        public SizeD(SizeD size)
        {
            width = size.width;
            height = size.height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='SizeD'/> class from the specified
        /// <see cref='PointD'/>.
        /// </summary>
        public SizeD(PointD pt)
        {
            width = pt.X;
            height = pt.Y;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='SizeD'/> class from the specified dimensions.
        /// </summary>
        public SizeD(double width, double height)
        {
            this.width = width;
            this.height = height;
        }

        /// <summary>
        /// Performs vector addition of two <see cref='SizeD'/> objects.
        /// </summary>
        public static SizeD operator +(SizeD sz1, SizeD sz2) => Add(sz1, sz2);

        /// <summary>
        /// Contracts a <see cref='SizeD'/> by another <see cref='SizeD'/>
        /// </summary>
        public static SizeD operator -(SizeD sz1, SizeD sz2) => Subtract(sz1, sz2);

        /// <summary>
        /// Multiplies <see cref="SizeD"/> by a <see cref="double"/> producing <see cref="SizeD"/>.
        /// </summary>
        /// <param name="left">Multiplier of type <see cref="double"/>.</param>
        /// <param name="right">Multiplicand of type <see cref="SizeD"/>.</param>
        /// <returns>Product of type <see cref="SizeD"/>.</returns>
        public static SizeD operator *(double left, SizeD right) => Multiply(right, left);

        /// <summary>
        /// Multiplies <see cref="SizeD"/> by a <see cref="double"/> producing <see cref="SizeD"/>.
        /// </summary>
        /// <param name="left">Multiplicand of type <see cref="SizeD"/>.</param>
        /// <param name="right">Multiplier of type <see cref="double"/>.</param>
        /// <returns>Product of type <see cref="SizeD"/>.</returns>
        public static SizeD operator *(SizeD left, double right) => Multiply(left, right);

        /// <summary>
        /// Divides <see cref="SizeD"/> by a <see cref="double"/> producing <see cref="SizeD"/>.
        /// </summary>
        /// <param name="left">Dividend of type <see cref="SizeD"/>.</param>
        /// <param name="right">Divisor of type <see cref="int"/>.</param>
        /// <returns>Result of type <see cref="SizeD"/>.</returns>
        public static SizeD operator /(SizeD left, double right)
            => new SizeD(left.width / right, left.height / right);

        /// <summary>
        /// Tests whether two <see cref='SizeD'/> objects are identical.
        /// </summary>
        public static bool operator ==(SizeD sz1, SizeD sz2) => sz1.Width == sz2.Width && sz1.Height == sz2.Height;

        /// <summary>
        /// Tests whether two <see cref='SizeD'/> objects are different.
        /// </summary>
        public static bool operator !=(SizeD sz1, SizeD sz2) => !(sz1 == sz2);

        /// <summary>
        /// Converts the specified <see cref='SizeD'/> to a <see cref='PointD'/>.
        /// </summary>
        public static explicit operator PointD(SizeD size) => new PointD(size.Width, size.Height);

        /// <summary>
        /// Tests whether this <see cref='SizeD'/> has zero width and height.
        /// </summary>
        [Browsable(false)]
        public readonly bool IsEmpty => width == 0 && height == 0;

        /// <summary>
        /// Represents the horizontal component of this <see cref='SizeD'/>.
        /// </summary>
        public double Width
        {
            readonly get => width;
            set => width = value;
        }

        /// <summary>
        /// Represents the vertical component of this <see cref='SizeD'/>.
        /// </summary>
        public double Height
        {
            readonly get => height;
            set => height = value;
        }

        /// <summary>
        /// Performs vector addition of two <see cref='SizeD'/> objects.
        /// </summary>
        public static SizeD Add(SizeD sz1, SizeD sz2) => new SizeD(sz1.Width + sz2.Width, sz1.Height + sz2.Height);

        /// <summary>
        /// Contracts a <see cref='SizeD'/> by another <see cref='SizeD'/>.
        /// </summary>
        public static SizeD Subtract(SizeD sz1, SizeD sz2) => new SizeD(sz1.Width - sz2.Width, sz1.Height - sz2.Height);

        /// <summary>
        /// Tests to see whether the specified object is a <see cref='SizeD'/>  with the same dimensions
        /// as this <see cref='SizeD'/>.
        /// </summary>
        public override readonly bool Equals(object obj) => obj is SizeD && Equals((SizeD)obj);

        /// <inheritdoc/>
        public readonly bool Equals(SizeD other) => this == other;

        /// <inheritdoc/>
        public override readonly int GetHashCode() => HashCode.Combine(Width, Height);

        /// <summary>
        /// Converts the <see cref="SizeD"/> to a <see cref="PointD"/>.
        /// </summary>
        /// <returns>A <see cref="PointD"/> converted from the <see cref="SizeD"/>.</returns>
        public readonly PointD ToPointD() => (PointD)this;

        /// <summary>
        /// Converts a <see cref='SizeD'/> to a <see cref='System.Drawing.Size'/> by performing a truncate operation on all the coordinates.
        /// </summary>
        /// <remarks>
        /// Adapted from Size.Truncate method at https://github.com/dotnet/runtime/blob/master/src/libraries/System.Drawing.Primitives/src/System/Drawing/Size.cs
        /// Licensed to the .NET Foundation under one or more agreements.
        /// The .NET Foundation licenses this file to you under the MIT license.
        /// See LICENSE.TXT at https://github.com/dotnet/runtime/blob/master/LICENSE.TXT
        /// </remarks>
        public readonly System.Drawing.Size ToSize() => new System.Drawing.Size(unchecked((int)Width), unchecked((int)Height));

        /// <summary>
        /// Creates a human-readable string that represents this <see cref='SizeD'/>.
        /// </summary>
        public override readonly string ToString() => "{Width=" + width.ToString() + ", Height=" + height.ToString() + "}";

        /// <summary>
        /// Multiplies <see cref="SizeD"/> by a <see cref="double"/> producing <see cref="SizeD"/>.
        /// </summary>
        /// <param name="size">Multiplicand of type <see cref="SizeD"/>.</param>
        /// <param name="multiplier">Multiplier of type <see cref="double"/>.</param>
        /// <returns>Product of type SizeD.</returns>
        private static SizeD Multiply(SizeD size, double multiplier) =>
            new SizeD(size.width * multiplier, size.height * multiplier);
    }
}
