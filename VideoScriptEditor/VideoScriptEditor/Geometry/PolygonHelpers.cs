using System;
using System.Collections.Generic;
using System.Windows;

namespace VideoScriptEditor.Geometry
{
    /// <summary>
    /// Helper methods for polygon geometries.
    /// </summary>
    public static class PolygonHelpers
    {
        /// <summary>
        /// Calculates the bounding <see cref="Rect"/> of a collection of <see cref="Point"/>s
        /// that make up a polygon.
        /// </summary>
        /// <remarks>
        /// Adapted from sample code posted by Kelly Thomas on Game Development Stack Exchange
        /// at https://gamedev.stackexchange.com/questions/70077/how-to-calculate-a-bounding-rectangle-of-a-polygon/70085#70085
        /// </remarks>
        /// <param name="points">A collection of <see cref="Point"/>s that make up the polygon.</param>
        /// <returns>The bounding <see cref="Rect"/> of the polygon.</returns>
        public static Rect GetBounds(IList<Point> points)
        {
            if (points?.Count == 0)
            {
                return Rect.Empty;
            }

            Point topLeft = new Point(double.MaxValue, double.MaxValue);
            Point bottomRight = new Point(double.MinValue, double.MinValue);

            foreach (Point point in points)
            {
                topLeft.X = Math.Min(topLeft.X, point.X);
                topLeft.Y = Math.Min(topLeft.Y, point.Y);
                bottomRight.X = Math.Max(bottomRight.X, point.X);
                bottomRight.Y = Math.Max(bottomRight.Y, point.Y);
            }

            return new Rect(topLeft, bottomRight);
        }
    }
}
