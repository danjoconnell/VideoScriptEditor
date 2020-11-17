/*
    Based on code from Gu.Wpf.Geometry, located at https://github.com/GuOrg/Gu.Wpf.Geometry
    Gu.Wpf.Geometry is licensed under the MIT license (MIT), Copyright (c) 2015 Johan Larsson.
    The full text of the license is available at https://github.com/GuOrg/Gu.Wpf.Geometry/blob/master/LICENSE.md
*/

using System;
using System.Globalization;
using System.Windows;

namespace VideoScriptEditor.Geometry
{
    public static class PointExt
    {
        public static Point WithOffset(this Point self, Vector direction, double distance)
        {
            return self + distance * direction;
        }

        public static double DistanceTo(this Point self, Point other)
        {
            return (self - other).Length;
        }

        public static Point Round(this Point self, int digits = 0)
        {
            return new Point(Math.Round(self.X, digits), Math.Round(self.Y, digits));
        }

        public static Vector VectorTo(this Point self, Point other)
        {
            return other - self;
        }

        public static Line LineTo(this Point self, Point other)
        {
            return new Line(self, other);
        }

        public static Point Closest(this Point self, Point p1, Point p2)
        {
            return self.DistanceTo(p1) < self.DistanceTo(p2) ? p1 : p2;
        }

        public static Point Closest(this Point self, Point p1, Point p2, Point p3, Point p4)
        {
            return self.Closest(self.Closest(p1, p2), self.Closest(p3, p4));
        }

        public static Point MidPoint(Point p1, Point p2)
        {
            return new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
        }

        public static string ToString(this Point? self, string format = "F1")
        {
            return self == null ? "null" : self.Value.ToString(format);
        }

        public static string ToString(this Point self, string format = "F1")
        {
            return $"{self.X.ToString(format, CultureInfo.InvariantCulture)},{self.Y.ToString(format, CultureInfo.InvariantCulture)}";
        }

        /*
            VideoScriptEditor specific extensions
        */

        /// <summary>
        /// Determines the angle of a straight line drawn between two <see cref="Point"/>s.
        /// </summary>
        /// <remarks>
        /// Based on sample code from https://web.archive.org/web/20190317110225/http://wikicode.wikidot.com/get-angle-of-line-between-two-points
        /// </remarks>
        /// <param name="p1">The first <see cref="Point"/>.</param>
        /// <param name="p2">The second <see cref="Point"/>.</param>
        /// <returns>The angle between the two <see cref="Point"/>s in degrees.</returns>
        public static double AngleTo(this Point p1, in Point p2)
        {
            double xDiff = p2.X - p1.X;
            double yDiff = p2.Y - p1.Y;
            return Math.Atan2(yDiff, xDiff) * (180 / Math.PI);
        }

        /// <summary>
        /// Linearly interpolates between two <see cref="Point"/>s based on the given weighting.
        /// </summary>
        /// <remarks>
        /// Adapted from MS.Internal.PresentationCore.AnimatedTypeHelpers, method 'InterpolatePoint'
        /// at https://github.com/dotnet/wpf/blob/aaf7f87d0845d6baf0a59f18d1efa4a37ccc63d0/src/Microsoft.DotNet.Wpf/src/PresentationCore/MS/internal/AnimatedTypeHelpers.cs#L101
        /// Licensed to the .NET Foundation under one or more agreements.
        /// The .NET Foundation licenses this file to you under the MIT license.
        /// See https://github.com/dotnet/runtime/blob/master/LICENSE.TXT for more information.
        /// </remarks>
        /// <param name="self">The first source <see cref="Point"/>.</param>
        /// <param name="other">The second source <see cref="Point"/>.</param>
        /// <param name="amount">Value indicating the weight of the second source <see cref="Point"/>.</param>
        /// <returns>The interpolated <see cref="Point"/>.</returns>
        public static Point LerpTo(this Point self, in Point other, double amount)
        {
            return self + ((other - self) * amount);
        }

        /// <summary>
        /// Scales the <see cref="Point"/> by the specified x and y factors.
        /// </summary>
        /// <param name="point">The source <see cref="Point"/>.</param>
        /// <param name="xScaleFactor">The scale factor in the x-direction.</param>
        /// <param name="yScaleFactor">The scale factor in the y-direction.</param>
        /// <returns>The scaled <see cref="Point"/>.</returns>
        public static Point Scale(this Point point, double xScaleFactor, double yScaleFactor)
        {
            return new Point(point.X * xScaleFactor, point.Y * yScaleFactor);
        }

        /// <summary>
        /// Clamps the <see cref="Point"/> to the specified bounding width and height.
        /// </summary>
        /// <param name="point">The source <see cref="Point"/></param>
        /// <param name="boundingWidth">The maximum X-coordinate value of the <paramref name="point"/>.</param>
        /// <param name="boundingHeight">The maximum Y-coordinate value of the <paramref name="point"/>.</param>
        /// <returns>The clamped <see cref="Point"/>.</returns>
        public static Point ClampToBounds(this Point point, double boundingWidth, double boundingHeight)
        {
            return new Point(Math.Clamp(point.X, 0d, boundingWidth),
                             Math.Clamp(point.Y, 0d, boundingHeight));
        }
    }
}
