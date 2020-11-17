/*
    Based on code from Gu.Wpf.Geometry, located at https://github.com/GuOrg/Gu.Wpf.Geometry
    Gu.Wpf.Geometry is licensed under the MIT license (MIT), Copyright (c) 2015 Johan Larsson.
    The full text of the license is available at https://github.com/GuOrg/Gu.Wpf.Geometry/blob/master/LICENSE.md
*/

namespace VideoScriptEditor.Geometry
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public struct Ellipse : IEquatable<Ellipse>
    {
        public readonly Point CenterPoint;
        public readonly double RadiusX;
        public readonly double RadiusY;

        public Ellipse(Rect rect)
        {
            Debug.Assert(!rect.IsEmpty, "!rect.IsEmpty");
            RadiusX = (rect.Right - rect.X) * 0.5;
            RadiusY = (rect.Bottom - rect.Y) * 0.5;
            CenterPoint = new Point(rect.X + RadiusX, rect.Y + RadiusY);
        }

        public Ellipse(Point centerPoint, double radiusX, double radiusY)
        {
            CenterPoint = centerPoint;
            RadiusX = radiusX;
            RadiusY = radiusY;
        }

        public bool IsZero => RadiusX <= 0 || RadiusY <= 0;

        private string DebuggerDisplay => $"{CenterPoint.ToString("F1")} rx: {RadiusX:F1} ry: {RadiusY:F1}";

        public static Ellipse CreateFromSize(Size renderSize)
        {
            var width = renderSize.Width;
            var height = renderSize.Height;
            var rx = width / 2;
            var ry = height / 2;
            return new Ellipse(new Point(rx, ry), rx, ry);
        }

        public static Ellipse Parse(string text)
        {
            var strings = text.Split(';');
            if (strings.Length != 3)
            {
                throw new ArgumentException();
            }

            var cp = Point.Parse(strings[0]);
            var rx = double.Parse(strings[1]);
            var ry = double.Parse(strings[2]);
            return new Ellipse(cp, rx, ry);
        }

        // Not sure if radius makes any sense here, not very important since public
        // http://math.stackexchange.com/a/687384/47614
        public double RadiusInDirection(Vector directionFromCenter)
        {
            var angle = Math.Atan2(directionFromCenter.Y, directionFromCenter.X);
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            var a = RadiusX;
            var b = RadiusY;
            var r2 = 1 / (cos * cos / (a * a) + sin * sin / (b * b));
            return Math.Sqrt(r2);
        }

        public Point PointOnCircumference(Vector directionFromCenter)
        {
            var r = RadiusInDirection(directionFromCenter);
            return CenterPoint + (r * directionFromCenter.Normalized());
        }

        public bool Contains(Point p)
        {
            var v = CenterPoint.VectorTo(p);
            var r = RadiusInDirection(v);
            return CenterPoint.DistanceTo(p) <= r;
        }

        public static Rect CalculateBounds(Ellipse ellipse) => CalculateBounds(ellipse.CenterPoint, ellipse.RadiusX, ellipse.RadiusY);

        public static Rect CalculateBounds(Point centerPoint, double radiusX, double radiusY)
        {
            return new Rect(
                centerPoint.X - radiusX,
                centerPoint.Y - radiusY,
                radiusX * 2d,
                radiusY * 2d
            );
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals([AllowNull] Ellipse other)
        {
            return CenterPoint == other.CenterPoint
                && RadiusX == other.RadiusX
                && RadiusY == other.RadiusY;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is Ellipse otherEllipse && Equals(otherEllipse);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(CenterPoint, RadiusX, RadiusY);
        }

        /// <summary>
        /// Compares two <see cref="Ellipse"/> instances for exact equality.
        /// </summary>
        /// <param name="left">The left hand side <see cref="Ellipse"/> instance to compare.</param>
        /// <param name="right">The right hand side <see cref="Ellipse"/> instance to compare.</param>
        /// <returns>
        /// <see langword="true"/> if the two <see cref="Ellipse"/> instances are exactly equal, otherwise <see langword="false"/>.
        /// </returns>
        public static bool operator ==(Ellipse left, Ellipse right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="Ellipse"/> instances for exact equality.
        /// </summary>
        /// <param name="left">The left hand side <see cref="Ellipse"/> instance to compare.</param>
        /// <param name="right">The right hand side <see cref="Ellipse"/> instance to compare.</param>
        /// <returns>
        /// <see langword="true"/> if the two <see cref="Ellipse"/> instances are exactly equal, otherwise <see langword="false"/>.
        /// </returns>
        public static bool operator !=(Ellipse left, Ellipse right)
        {
            return !(left == right);
        }
    }
}