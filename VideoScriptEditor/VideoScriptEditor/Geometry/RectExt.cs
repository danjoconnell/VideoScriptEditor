/*
    Based on code from Gu.Wpf.Geometry, located at https://github.com/GuOrg/Gu.Wpf.Geometry
    Gu.Wpf.Geometry is licensed under the MIT license (MIT), Copyright (c) 2015 Johan Larsson.
    The full text of the license is available at https://github.com/GuOrg/Gu.Wpf.Geometry/blob/master/LICENSE.md
*/
using System;
using System.Windows;

namespace VideoScriptEditor.Geometry
{
    public static class RectExt
    {
        public static Line TopLine(this Rect rect)
        {
            return new Line(rect.TopLeft, rect.TopRight);
        }

        public static Line BottomLine(this Rect rect)
        {
            return new Line(rect.BottomRight, rect.BottomLeft);
        }

        public static Line LeftLine(this Rect rect)
        {
            return new Line(rect.BottomLeft, rect.TopLeft);
        }

        public static Line RightLine(this Rect rect)
        {
            return new Line(rect.TopRight, rect.BottomRight);
        }

        public static Point CenterPoint(this Rect rect)
        {
            return PointExt.MidPoint(rect.TopLeft, rect.BottomRight);
        }

        public static Point ClosestCornerPoint(this Rect rect, Point p)
        {
            return p.Closest(rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft);
        }

        /*
            VideoScriptEditor specific extensions
        */

        /// <summary>
        /// Gets the position of the top-center point on the rectangle.
        /// </summary>
        /// <param name="rect">The source <see cref="Rect"/>.</param>
        /// <returns>The position of the top-center point on the rectangle.</returns>
        public static Point TopCenter(this Rect rect)
        {
            return new Point(rect.Left + (rect.Width / 2), rect.Top);
        }

        /// <summary>
        /// Gets the position of the bottom-center point on the rectangle.
        /// </summary>
        /// <param name="rect">The source <see cref="Rect"/>.</param>
        /// <returns>The position of the bottom-center point on the rectangle.</returns>
        public static Point BottomCenter(this Rect rect)
        {
            return new Point(rect.Left + (rect.Width / 2), rect.Bottom);
        }

        /// <summary>
        /// Gets the position of the center-left point on the rectangle.
        /// </summary>
        /// <param name="rect">The source <see cref="Rect"/>.</param>
        /// <returns>The position of the center-left point on the rectangle.</returns>
        public static Point CenterLeft(this Rect rect)
        {
            return new Point(rect.Left, rect.Top + (rect.Height / 2));
        }

        /// <summary>
        /// Gets the position of the center-right point on the rectangle.
        /// </summary>
        /// <param name="rect">The source <see cref="Rect"/>.</param>
        /// <returns>The position of the center-right point on the rectangle.</returns>
        public static Point CenterRight(this Rect rect)
        {
            return new Point(rect.Right, rect.Top + (rect.Height / 2));
        }

        /// <summary>
        /// Constrains the <see cref="Point"/> within the rectangle.
        /// </summary>
        /// <param name="rect">The source <see cref="Rect"/>.</param>
        /// <param name="point">The <see cref="Point"/> to constrain.</param>
        /// <returns>The constrained <see cref="Point"/>.</returns>
        public static Point ConstrainPoint(this Rect rect, in Point point) => new Point(
            x: Math.Clamp(point.X, rect.Left, rect.Right),
            y: Math.Clamp(point.Y, rect.Top, rect.Bottom)
        );

        /// <summary>
        /// Returns a <see cref="Rect"/> that is offset from the center of this <see cref="Rect"/> by the
        /// specified <see cref="Vector"/>, constrained by the specified bounding <see cref="Rect"/>.
        /// </summary>
        /// <param name="rect">The source <see cref="Rect"/>.</param>
        /// <param name="centerOffset">
        /// A <see cref="Vector"/> that specifies the amount to offset the center of this <see cref="Rect"/>.
        /// </param>
        /// <param name="boundingRect">The constraining bounding <see cref="Rect"/>.</param>
        /// <returns>The resulting <see cref="Rect"/>.</returns>
        public static Rect OffsetFromCenterWithinBounds(this Rect rect, in Vector centerOffset, in Rect boundingRect)
        {
            if (rect.IsEmpty)
            {
                return rect;
            }

            Point currentCenterPoint = CenterPoint(rect);

            // Offset center point, constraining to bounding rect
            Point offsetCenterPoint = new Point(
                Math.Clamp(currentCenterPoint.X + centerOffset.X, boundingRect.Left + (rect.Width / 2d), boundingRect.Right - (rect.Width / 2d)),
                Math.Clamp(currentCenterPoint.Y + centerOffset.Y, boundingRect.Top + (rect.Height / 2d), boundingRect.Bottom - (rect.Height / 2d))
            );

            // Offset rect
            return Rect.Offset(rect, offsetCenterPoint - currentCenterPoint);
        }

        /// <summary>
        /// Linearly interpolates between two <see cref="Rect"/>s based on the given weighting.
        /// </summary>
        /// <remarks>
        /// Adapted from MS.Internal.PresentationCore.AnimatedTypeHelpers, method 'InterpolateRect'
        /// at https://github.com/dotnet/wpf/blob/aaf7f87d0845d6baf0a59f18d1efa4a37ccc63d0/src/Microsoft.DotNet.Wpf/src/PresentationCore/MS/internal/AnimatedTypeHelpers.cs#L116
        /// Licensed to the .NET Foundation under one or more agreements.
        /// The .NET Foundation licenses this file to you under the MIT license.
        /// See https://github.com/dotnet/runtime/blob/master/LICENSE.TXT for more information.
        /// </remarks>
        /// <param name="self">The first source <see cref="Rect"/>.</param>
        /// <param name="other">The second source <see cref="Rect"/>.</param>
        /// <param name="amount">Value indicating the weight of the second source <see cref="Rect"/>.</param>
        /// <returns>The interpolated <see cref="Rect"/>.</returns>
        public static Rect LerpTo(this Rect self, in Rect other, double amount) => new Rect
        {
            // self + ((self - other) * amount)
            Location = new Point(self.Location.X + ((other.Location.X - self.Location.X) * amount),
                                 self.Location.Y + ((other.Location.Y - self.Location.Y) * amount)),
            Size = new Size(self.Size.Width + ((other.Size.Width - self.Size.Width) * amount),
                            self.Size.Height + ((other.Size.Height - self.Size.Height) * amount))
        };
    }
}
