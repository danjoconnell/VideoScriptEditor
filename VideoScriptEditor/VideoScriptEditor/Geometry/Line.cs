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
    public struct Line : IEquatable<Line>
    {
        public readonly Point StartPoint;
        public readonly Point EndPoint;

        public Line(Point startPoint, Point endPoint)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }

        public Point MidPoint
        {
            get
            {
                var x = (StartPoint.X + EndPoint.X) / 2;
                var y = (StartPoint.Y + EndPoint.Y) / 2;
                return new Point(x, y);
            }
        }

        public double Length => (EndPoint - StartPoint).Length;

        public Vector Direction
        {
            get
            {
                var v = EndPoint - StartPoint;
                v.Normalize();
                return v;
            }
        }

        public Vector PerpendicularDirection
        {
            get
            {
                var direction = Direction;
                return new Vector(direction.Y, -direction.X);
            }
        }

        private string DebuggerDisplay => $"{StartPoint.ToString("F1")} -> {EndPoint.ToString("F1")} length: {Length:F1}";

        public override string ToString() => ToString(string.Empty);

        public string ToString(string format) => $"{StartPoint.ToString(format)}; {EndPoint.ToString(format)}";

        public static Line Parse(string text)
        {
            var strings = text.Split(';');
            if (strings.Length != 2)
            {
                throw new ArgumentException();
            }

            var sp = Point.Parse(strings[0]);
            var ep = Point.Parse(strings[1]);
            return new Line(sp, ep);
        }

        public Line RotateAroundStartPoint(double angleInDegrees)
        {
            var v = EndPoint - StartPoint;
            v = v.Rotate(angleInDegrees);
            var ep = StartPoint + v;
            return new Line(StartPoint, ep);
        }

        public Line Flip()
        {
            return new Line(EndPoint, StartPoint);
        }

        public Line Offset(double distance)
        {
            var v = PerpendicularDirection;
            var sp = StartPoint.WithOffset(v, distance);
            var ep = EndPoint.WithOffset(v, distance);
            return new Line(sp, ep);
        }

        public bool IsPointOnLine(Point p)
        {
            if (StartPoint.DistanceTo(p) < Constants.Tolerance)
            {
                return true;
            }

            var v = p - StartPoint;
            var angleBetween = Vector.AngleBetween(Direction, v);
            if (Math.Abs(angleBetween) > Constants.Tolerance)
            {
                return false;
            }

            return v.Length <= Length + Constants.Tolerance;
        }

        public Point? TrimTo(Point p)
        {
            if (IsPointOnLine(p))
            {
                return p;
            }

            var v = StartPoint.VectorTo(p);
            if (Math.Abs(v.AngleTo(Direction) % 180) > Constants.Tolerance)
            {
                return null;
            }

            var dp = v.DotProduct(Direction);
            return dp < 0
                       ? StartPoint
                       : EndPoint;
        }

        public Point Project(Point p)
        {
            var toPoint = StartPoint.VectorTo(p);
            var dotProdcut = toPoint.DotProduct(Direction);
            var projected = StartPoint + dotProdcut * Direction;
            return projected;
        }

        public Line? TrimOrExtendEndWith(Line other)
        {
            if (EndPoint.DistanceTo(other.StartPoint) < Constants.Tolerance)
            {
                return this;
            }

            var ip = IntersectionPoint(this, other, mustBeBetweenStartAndEnd: false);
            if (ip == null)
            {
                return this;
            }

            return new Line(StartPoint, ip.Value);
        }

        public Line? TrimOrExtendStartWith(Line other)
        {
            if (StartPoint.DistanceTo(other.EndPoint) < Constants.Tolerance)
            {
                return this;
            }

            var ip = IntersectionPoint(this, other, mustBeBetweenStartAndEnd: false);
            if (ip == null)
            {
                return null;
            }

            return new Line(ip.Value, EndPoint);
        }

        public Point? IntersectWith(Line other, bool mustBeBetweenStartAndEnd)
        {
            return IntersectionPoint(this, other, mustBeBetweenStartAndEnd);
        }

        public Point? ClosestIntersection(Rect rectangle)
        {
            var quadrant = rectangle.Contains(StartPoint)
                ? Direction.Quadrant()
                : Direction.Negated().Quadrant();

            switch (quadrant)
            {
                case Quadrant.NegativeXPositiveY:
                    return IntersectionPoint(rectangle.LeftLine(), this, mustBeBetweenStartAndEnd: true) ??
                           IntersectionPoint(rectangle.BottomLine(), this, mustBeBetweenStartAndEnd: true);
                case Quadrant.PositiveXPositiveY:
                    return IntersectionPoint(rectangle.RightLine(), this, mustBeBetweenStartAndEnd: true) ??
                           IntersectionPoint(rectangle.BottomLine(), this, mustBeBetweenStartAndEnd: true);
                case Quadrant.PositiveXNegativeY:
                    return IntersectionPoint(rectangle.RightLine(), this, mustBeBetweenStartAndEnd: true) ??
                           IntersectionPoint(rectangle.TopLine(), this, mustBeBetweenStartAndEnd: true);
                case Quadrant.NegativeXNegativeY:
                    return IntersectionPoint(rectangle.LeftLine(), this, mustBeBetweenStartAndEnd: true) ??
                           IntersectionPoint(rectangle.TopLine(), this, mustBeBetweenStartAndEnd: true);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public double DistanceTo(Point p)
        {
            return Project(p).DistanceTo(p);
        }

        public double DistanceToPointOnLine(Point p)
        {
            var toPoint = StartPoint.VectorTo(p);
            var dotProdcut = toPoint.DotProduct(Direction);
            var pointOnLine = StartPoint + dotProdcut * Direction;
            return pointOnLine.DistanceTo(p);
        }

        public Line? PerpendicularLineTo(Point p)
        {
            if (IsPointOnLine(p))
            {
                return null;
            }

            var startPoint = Project(p);
            return new Line(startPoint, p);
        }

        // http://geomalgorithms.com/a05-_intersect-1.html#intersect2D_2Segments()
        private static Point? IntersectionPoint(Line l1, Line l2, bool mustBeBetweenStartAndEnd)
        {
            var u = l1.Direction;
            var v = l2.Direction;
            var w = l1.StartPoint - l2.StartPoint;
            var d = Perp(u, v);
            if (Math.Abs(d) < Constants.Tolerance)
            {
                // parallel lines
                return null;
            }

            var sI = Perp(v, w) / d;
            var p = l1.StartPoint + sI * u;
            if (mustBeBetweenStartAndEnd)
            {
                if (l1.IsPointOnLine(p) && l2.IsPointOnLine(p))
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

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals([AllowNull] Line other)
        {
            return StartPoint == other.StartPoint && EndPoint == other.EndPoint;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is Line otherLine && Equals(otherLine);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(StartPoint, EndPoint);
        }

        /// <summary>
        /// Compares two <see cref="Line"/> instances for exact equality.
        /// </summary>
        /// <param name="left">The left hand side <see cref="Line"/> instance to compare.</param>
        /// <param name="right">The right hand side <see cref="Line"/> instance to compare.</param>
        /// <returns>
        /// <see langword="true"/> if the two <see cref="Line"/> instances are exactly equal, otherwise <see langword="false"/>.
        /// </returns>
        public static bool operator ==(Line left, Line right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="Line"/> instances for exact inequality.
        /// </summary>
        /// <param name="left">The left hand side <see cref="Line"/> instance to compare.</param>
        /// <param name="right">The right hand side <see cref="Line"/> instance to compare.</param>
        /// <returns>
        /// <see langword="true"/> if the two <see cref="Line"/> instances are exactly unequal, otherwise <see langword="false"/>.
        /// </returns>
        public static bool operator !=(Line left, Line right)
        {
            return !(left == right);
        }
    }
}
