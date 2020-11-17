using System;
using System.ComponentModel;
using System.Windows;

namespace VideoScriptEditor.Geometry
{
    /// <summary>
    /// Represents a rectangle made up of four corner points.
    /// </summary>
    public struct RectanglePolygon : IEquatable<RectanglePolygon>
    {
        /// <summary>
        /// The top-left corner point of the rectangle.
        /// </summary>
        public Point TopLeft { get; }

        /// <summary>
        /// The top-right corner point of the rectangle.
        /// </summary>
        public Point TopRight { get; }

        /// <summary>
        /// The bottom-right corner point of the rectangle.
        /// </summary>
        public Point BottomRight { get; }

        /// <summary>
        /// The bottom-left corner point of the rectangle.
        /// </summary>
        public Point BottomLeft { get; }

        /// <summary>
        /// The calculated top-center point of the rectangle.
        /// </summary>
        public Point TopCenter => PointExt.MidPoint(TopLeft, TopRight);

        /// <summary>
        /// The calculated center-left point of the rectangle.
        /// </summary>
        public Point CenterLeft => PointExt.MidPoint(TopLeft, BottomLeft);

        /// <summary>
        /// The calculated center point of the rectangle.
        /// </summary>
        public Point Center => PointExt.MidPoint(TopLeft, BottomRight);

        /// <summary>
        /// The calculated center-right point of the rectangle.
        /// </summary>
        public Point CenterRight => PointExt.MidPoint(TopRight, BottomRight);

        /// <summary>
        /// The calculated bottom-center point of the rectangle.
        /// </summary>
        public Point BottomCenter => PointExt.MidPoint(BottomLeft, BottomRight);

        /// <summary>
        /// The calculated width of the rectangle.
        /// </summary>
        public double Width => CenterLeft.DistanceTo(CenterRight);

        /// <summary>
        /// The calculated height of the rectangle.
        /// </summary>
        public double Height => TopCenter.DistanceTo(BottomCenter);

        /// <summary>
        /// The calculated rotation angle of the rectangle in degrees.
        /// </summary>
        public double Angle => CenterLeft.AngleTo(Center);

        /// <summary>
        /// The calculated axis-aligned bounding box of the rectangle.
        /// </summary>
        public Rect Bounds
        {
            get
            {
                Point[] cornerPoints = new Point[] { TopLeft, TopRight, BottomRight, BottomLeft };
                return PolygonHelpers.GetBounds(cornerPoints);
            }
        }

        /// <summary>
        /// A <see cref="Line"/> representing the top edge (clockwise) of the rectangle.
        /// </summary>
        public Line TopEdge => new Line(TopLeft, TopRight);

        /// <summary>
        /// A <see cref="Line"/> representing the right edge (clockwise) of the rectangle.
        /// </summary>
        public Line RightEdge => new Line(TopRight, BottomRight);

        /// <summary>
        /// A <see cref="Line"/> representing the bottom edge (clockwise) of the rectangle.
        /// </summary>
        public Line BottomEdge => new Line(BottomRight, BottomLeft);

        /// <summary>
        /// A <see cref="Line"/> representing the left edge (clockwise) of the rectangle.
        /// </summary>
        public Line LeftEdge => new Line(BottomLeft, TopLeft);

        /// <summary>
        /// Creates a new <see cref="RectanglePolygon"/> instance from four corner <see cref="Point"/>s.
        /// </summary>
        /// <param name="topLeft">The top-left corner <see cref="Point"/>.</param>
        /// <param name="topRight">The top-right corner <see cref="Point"/>.</param>
        /// <param name="bottomRight">The bottom-right corner <see cref="Point"/>.</param>
        /// <param name="bottomLeft">The bottom-left corner <see cref="Point"/>.</param>
        public RectanglePolygon(Point topLeft, Point topRight, Point bottomRight, Point bottomLeft)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomRight = bottomRight;
            BottomLeft = bottomLeft;
        }

        /// <summary>
        /// Creates a new <see cref="RectanglePolygon"/> instance
        /// by rotating an axis-aligned <see cref="Rect"/> to a specified angle.
        /// </summary>
        /// <param name="axisAlignedRect">The axis-aligned <see cref="Rect"/> to rotate.</param>
        /// <param name="rotationAngle">The rotation angle (in degrees).</param>
        public RectanglePolygon(Rect axisAlignedRect, double rotationAngle)
        {
            Point[] rectPoints = new[] { axisAlignedRect.TopLeft, axisAlignedRect.TopRight, axisAlignedRect.BottomRight, axisAlignedRect.BottomLeft };

            if (Math.Abs(rotationAngle) != 0d)
            {
                System.Windows.Media.Matrix rotationMatrix = MatrixFactory.CreateRotationMatrix(rotationAngle, axisAlignedRect.CenterPoint());
                rotationMatrix.Transform(rectPoints);
            }

            TopLeft = rectPoints[0];
            TopRight = rectPoints[1];
            BottomRight = rectPoints[2];
            BottomLeft = rectPoints[3];
        }

        /// <summary>
        /// Gets the closest projected <see cref="Point"/> perpendicular to the middle of the specified edge of the rectangle
        /// for a given target <see cref="Point"/>.
        /// </summary>
        /// <param name="targetPoint">The target <see cref="Point"/>.</param>
        /// <param name="rectangleEdge">
        /// A <see cref="RectangleSide"/> value describing the edge of the rectangle to project perpendicularly.
        /// </param>
        /// <returns>
        /// The closest projected <see cref="Point"/> perpendicular to the middle of the <paramref name="rectangleEdge"/> for the <paramref name="targetPoint"/>.
        /// </returns>
        public Point ClosestPerpendicularPointToEdgeMidPoint(Point targetPoint, RectangleSide rectangleEdge)
        {
            Line edge = GetEdge(rectangleEdge);
            Ray ray = new Ray(Center, edge.PerpendicularDirection);
            return ray.Project(targetPoint);
        }

        /// <summary>
        /// Gets the distance to the specified rectangle edge from a <see cref="Point"/> perpendicular to the middle of the specified rectangle edge.
        /// </summary>
        /// <param name="perpendicularPoint">A <see cref="Point"/> perpendicular to the <paramref name="rectangleEdge"/>.</param>
        /// <param name="rectangleEdge">
        /// A <see cref="RectangleSide"/> value describing the edge of the rectangle perpendicular to the <paramref name="perpendicularPoint"/>.
        /// </param>
        /// <returns>The distance from the <paramref name="perpendicularPoint"/> to the <paramref name="rectangleEdge"/>.</returns>
        public double DistanceFromPerpendicularPointToEdgeMidPoint(Point perpendicularPoint, RectangleSide rectangleEdge)
        {
            Line edge = GetEdge(rectangleEdge);
            double distance = edge.MidPoint.DistanceTo(perpendicularPoint);
            Point center = Center;
            return (center.DistanceTo(edge.MidPoint) > center.DistanceTo(perpendicularPoint)) ? -distance : distance;
        }

        /// <summary>
        /// De-rotates the <see cref="RectanglePolygon"/>, converting it into an axis-aligned <see cref="Rect"/>.
        /// </summary>
        /// <returns>A de-rotated axis-aligned <see cref="Rect"/>.</returns>
        public Rect ToDerotatedAxisAlignedRect()
        {
            double angle = Angle;
            if (Math.Abs(angle) == 0d)
            {
                return new Rect(TopLeft, BottomRight);
            }
            else
            {
                System.Windows.Media.Matrix rotationMatrix = MatrixFactory.CreateRotationMatrix(-angle, Center);
                Point[] rectPoints = new[] { TopLeft, BottomRight };
                rotationMatrix.Transform(rectPoints);

                return new Rect(rectPoints[0], rectPoints[1]);
            }
        }

        /// <summary>
        /// Gets a <see cref="Line"/> representing the edge of the rectangle
        /// corresponding to the specified <see cref="RectangleSide"/> value.
        /// </summary>
        /// <param name="rectangleEdge">
        /// A <see cref="RectangleSide"/> value describing the edge of the rectangle.
        /// </param>
        /// <returns>A <see cref="Line"/> representing an edge of the rectangle.</returns>
        private Line GetEdge(RectangleSide rectangleEdge) => rectangleEdge switch
        {
            RectangleSide.Left => LeftEdge,
            RectangleSide.Top => TopEdge,
            RectangleSide.Right => RightEdge,
            RectangleSide.Bottom => BottomEdge,
            _ => throw new InvalidEnumArgumentException(nameof(rectangleEdge)),
        };

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(RectanglePolygon other)
        {
            return TopLeft == other.TopLeft
                && TopRight == other.TopRight
                && BottomRight == other.BottomRight
                && BottomLeft == other.BottomLeft;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is RectanglePolygon otherRectanglePolygon && Equals(otherRectanglePolygon);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(TopLeft, TopRight, BottomRight, BottomLeft);
        }

        /// <summary>
        /// Compares two <see cref="RectanglePolygon"/> instances for exact equality.
        /// </summary>
        /// <param name="left">The left hand side <see cref="RectanglePolygon"/> instance to compare.</param>
        /// <param name="right">The right hand side <see cref="RectanglePolygon"/> instance to compare.</param>
        /// <returns>
        /// <see langword="true"/> if the two <see cref="RectanglePolygon"/> instances are exactly equal, otherwise <see langword="false"/>.
        /// </returns>
        public static bool operator ==(RectanglePolygon left, RectanglePolygon right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="RectanglePolygon"/> instances for exact inequality.
        /// </summary>
        /// <param name="left">The left hand side <see cref="RectanglePolygon"/> instance to compare.</param>
        /// <param name="right">The right hand side <see cref="RectanglePolygon"/> instance to compare.</param>
        /// <returns>
        /// <see langword="true"/> if the two <see cref="RectanglePolygon"/> instances are exactly unequal, otherwise <see langword="false"/>.
        /// </returns>
        public static bool operator !=(RectanglePolygon left, RectanglePolygon right)
        {
            return !(left == right);
        }
    }
}
