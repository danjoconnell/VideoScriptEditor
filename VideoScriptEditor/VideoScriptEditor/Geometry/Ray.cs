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
    public struct Ray : IEquatable<Ray>
    {
        public readonly Point Point;
        public readonly Vector Direction;

        public Ray(Point point, Vector direction)
        {
            Point = point;
            Direction = direction.Normalized();
        }

        private string DebuggerDisplay => $"{Point.ToString("F1")}, {Direction.ToString("F1")} angle: {Direction.AngleToPositiveX():F1}";

        public static Ray Parse(string text)
        {
            var strings = text.Split(';');
            if (strings.Length != 2)
            {
                throw new ArgumentException();
            }

            var p = Point.Parse(strings[0]);
            var v = Vector.Parse(strings[1]).Normalized();
            return new Ray(p, v);
        }

        public bool IsPointOn(Point p)
        {
            if (Point.DistanceTo(p) < Constants.Tolerance)
            {
                return true;
            }

            var angle = Point.VectorTo(p).AngleTo(Direction);
            return Math.Abs(angle) < Constants.Tolerance;
        }

        public Point Project(Point p)
        {
            var toPoint = Point.VectorTo(p);
            var dotProduct = toPoint.DotProduct(Direction);
            var projected = Point + dotProduct * Direction;
            return projected;
        }

        public Point? FirstIntersectionWith(Rect rectangle)
        {
            var quadrant = rectangle.Contains(Point)
                ? Direction.Quadrant()
                : Direction.Negated().Quadrant();

            switch (quadrant)
            {
                case Quadrant.NegativeXPositiveY:
                    return IntersectionPoint(this, rectangle.LeftLine(), mustBeBetweenStartAndEnd: true) ??
                           IntersectionPoint(this, rectangle.BottomLine(), mustBeBetweenStartAndEnd: true);
                case Quadrant.PositiveXPositiveY:
                    return IntersectionPoint(this, rectangle.RightLine(), mustBeBetweenStartAndEnd: true) ??
                           IntersectionPoint(this, rectangle.BottomLine(), mustBeBetweenStartAndEnd: true);
                case Quadrant.PositiveXNegativeY:
                    return IntersectionPoint(this, rectangle.RightLine(), mustBeBetweenStartAndEnd: true) ??
                           IntersectionPoint(this, rectangle.TopLine(), mustBeBetweenStartAndEnd: true);
                case Quadrant.NegativeXNegativeY:
                    return IntersectionPoint(this, rectangle.LeftLine(), mustBeBetweenStartAndEnd: true) ??
                           IntersectionPoint(this, rectangle.TopLine(), mustBeBetweenStartAndEnd: true);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // http://www.mare.ee/indrek/misc/2d.pdf
        public Point? FirstIntersectionWith(Ellipse ellipse)
        {
            if (Math.Abs(ellipse.CenterPoint.X) < Constants.Tolerance && Math.Abs(ellipse.CenterPoint.Y) < Constants.Tolerance)
            {
                return FirstIntersectionWithEllipseCenteredAtOrigin(Point, Direction, ellipse.RadiusX, ellipse.RadiusY);
            }

            var offset = new Point(0, 0).VectorTo(ellipse.CenterPoint);
            var ip = FirstIntersectionWithEllipseCenteredAtOrigin(Point - offset, Direction, ellipse.RadiusX, ellipse.RadiusY);
            return ip + offset;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("StyleCop", "SA1312")]
        private static Point? FirstIntersectionWithEllipseCenteredAtOrigin(Point startPoint, Vector direction, double a, double b)
        {
            var nx = direction.X;
            var nx2 = nx * nx;
            var ny = direction.Y;
            var ny2 = ny * ny;
            var x0 = startPoint.X;
            var x02 = x0 * x0;
            var y0 = startPoint.Y;
            var y02 = y0 * y0;
            var a2 = a * a;
            var b2 = b * b;
            var A = nx2 * b2 + ny2 * a2;
            if (Math.Abs(A) < Constants.Tolerance)
            {
                return null;
            }

            var B = 2 * x0 * nx * b2 + 2 * y0 * ny * a2;
            var C = x02 * b2 + y02 * a2 - a2 * b2;
            var d = B * B - 4 * A * C;
            if (d < 0)
            {
                return null;
            }

            var sqrt = Math.Sqrt(d);
            var s = (-B - sqrt) / (2 * A);
            if (s < 0)
            {
                s = (-B + sqrt) / (2 * A);
                return s > 0
                    ? new Point(x0, y0) + s * direction
                    : (Point?)null;
            }

            return new Point(x0, y0) + s * direction;
        }

        // http://geomalgorithms.com/a05-_intersect-1.html#intersect2D_2Segments()
        private static Point? IntersectionPoint(Ray ray, Line l2, bool mustBeBetweenStartAndEnd)
        {
            var u = ray.Direction;
            var v = l2.Direction;
            var w = ray.Point - l2.StartPoint;
            var d = Perp(u, v);
            if (Math.Abs(d) < Constants.Tolerance)
            {
                // parallel lines
                return null;
            }

            var sI = Perp(v, w) / d;
            var p = ray.Point + sI * u;
            if (mustBeBetweenStartAndEnd)
            {
                if (ray.IsPointOn(p) && l2.IsPointOnLine(p))
                {
                    return p;
                }

                return null;
            }

            return p;
        }

        private static double Perp(Vector u, Vector v)
        {
            return u.X * v.Y - u.Y * v.X;
        }

        /*
            VideoScriptEditor specific additions
        */

        /// <summary>
        /// Gets the point of intersection between this <see cref="Ray"/> and another.
        /// </summary>
        /// <param name="other">The intersecting <see cref="Ray"/>.</param>
        /// <returns>The intersection <see cref="System.Windows.Point"/>.</returns>
        public Point? IntersectWith(in Ray other)
        {
            Vector u = Direction;
            Vector v = other.Direction;
            Vector w = Point - other.Point;
            double d = Perp(u, v);
            if (Math.Abs(d) < Constants.Tolerance)
            {
                // parallel lines
                Debug.Fail($"Parallel lines for this[{DebuggerDisplay}], other[{other.DebuggerDisplay}].");
                return null;
            }

            double sI = Perp(v, w) / d;
            return Point + sI * u;
        }

        /// <summary>
        /// Gets the point of intersection between this <see cref="Ray"/> and another,
        /// within a bounding <see cref="Rect"/>.
        /// </summary>
        /// <param name="other">The intersecting <see cref="Ray"/>.</param>
        /// <param name="bounds">The bounding <see cref="Rect"/>.</param>
        /// <param name="intersectionPointOutOfBoundsDistance">
        /// (Out) If the intersection point is outside of the bounding <see cref="Rect"/>, the bounds overage distance.
        /// </param>
        /// <returns>The intersection <see cref="System.Windows.Point"/>.</returns>
        public Point? IntersectWithinBounds(in Ray other, in Rect bounds, out double intersectionPointOutOfBoundsDistance)
        {
            intersectionPointOutOfBoundsDistance = 0d;

            Point? intersectPoint = IntersectWith(other);
            Debug.Assert(intersectPoint.HasValue);
            if (!intersectPoint.HasValue)
            {
                return null;
            }

            if (bounds.Contains(intersectPoint.Value))
            {
                return intersectPoint.Value;
            }
            else
            {
                Point? p = other.FirstIntersectionWith(bounds);
                Debug.Assert(p.HasValue);
                if (!p.HasValue)
                {
                    return null;
                }

                intersectionPointOutOfBoundsDistance = p.Value.DistanceTo(intersectPoint.Value);

                return p.Value;
            }
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals([AllowNull] Ray other)
        {
            return Point == other.Point && Direction == other.Direction;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is Ray otherRay && Equals(otherRay);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Point, Direction);
        }

        /// <summary>
        /// Compares two <see cref="Ray"/> instances for exact equality.
        /// </summary>
        /// <param name="left">The left hand side <see cref="Ray"/> instance to compare.</param>
        /// <param name="right">The right hand side <see cref="Ray"/> instance to compare.</param>
        /// <returns>
        /// <see langword="true"/> if the two <see cref="Ray"/> instances are exactly equal, otherwise <see langword="false"/>.
        /// </returns>
        public static bool operator ==(Ray left, Ray right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="Ray"/> instances for exact inequality.
        /// </summary>
        /// <param name="left">The left hand side <see cref="Ray"/> instance to compare.</param>
        /// <param name="right">The right hand side <see cref="Ray"/> instance to compare.</param>
        /// <returns>
        /// <see langword="true"/> if the two <see cref="Ray"/> instances are exactly unequal, otherwise <see langword="false"/>.
        /// </returns>
        public static bool operator !=(Ray left, Ray right)
        {
            return !(left == right);
        }
    }
}
