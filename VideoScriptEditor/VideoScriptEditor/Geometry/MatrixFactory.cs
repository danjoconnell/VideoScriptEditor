/*
    Adapted from https://github.com/dotnet/runtime/blob/master/src/libraries/System.Private.CoreLib/src/System/Numerics/Matrix3x2.cs

    Licensed to the .NET Foundation under one or more agreements.
    The .NET Foundation licenses this file to you under the MIT license.
    See LICENSE.TXT at https://github.com/dotnet/runtime/blob/master/LICENSE.TXT for more information.
*/

using System;
using System.Windows;
using System.Windows.Media;

namespace VideoScriptEditor.Geometry
{
    /// <summary>
    /// A factory for creating <see cref="Matrix"/> instances.
    /// </summary>
    public static class MatrixFactory
    {
        /// <summary>
        /// Creates a rotation matrix using the given rotation in degrees and a center point.
        /// </summary>
        /// <param name="angleInDegrees">The amount of rotation, in degrees.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>A rotation matrix.</returns>
        /// <remarks>
        /// Adapted from source code for <see cref="System.Numerics.Matrix3x2.CreateRotation(float, System.Numerics.Vector2)"/>.
        /// </remarks>
        public static Matrix CreateRotationMatrix(double angleInDegrees, Point centerPoint = default)
        {
            double radians = angleInDegrees * Constants.DegToRad;
            radians = Math.IEEERemainder(radians, Math.PI * 2);

            double c, s;

            const double epsilon = 0.001d * Math.PI / 180d;     // 0.1% of a degree

            if (radians > -epsilon && radians < epsilon)
            {
                // Exact case for zero rotation.
                c = 1;
                s = 0;
            }
            else if (radians > Math.PI / 2 - epsilon && radians < Math.PI / 2 + epsilon)
            {
                // Exact case for 90 degree rotation.
                c = 0;
                s = 1;
            }
            else if (radians < -Math.PI + epsilon || radians > Math.PI - epsilon)
            {
                // Exact case for 180 degree rotation.
                c = -1;
                s = 0;
            }
            else if (radians > -Math.PI / 2 - epsilon && radians < -Math.PI / 2 + epsilon)
            {
                // Exact case for 270 degree rotation.
                c = 0;
                s = -1;
            }
            else
            {
                // Arbitrary rotation.
                c = Math.Cos(radians);
                s = Math.Sin(radians);
            }

            double x = centerPoint.X * (1 - c) + centerPoint.Y * s;
            double y = centerPoint.Y * (1 - c) - centerPoint.X * s;

            // [  c  s ]
            // [ -s  c ]
            // [  x  y ]
            return new Matrix(m11: c, m12: s,
                              m21: -s, m22: c,
                              offsetX: x, offsetY: y);
        }

        /// <summary>
        /// Creates a scale matrix that is offset by a given center point.
        /// </summary>
        /// <param name="xScale">Value to scale by on the X-axis.</param>
        /// <param name="yScale">Value to scale by on the Y-axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>A scaling matrix.</returns>
        /// <remarks>
        /// Adapted from source code for <see cref="System.Numerics.Matrix3x2.CreateScale(float, float, System.Numerics.Vector2)"/>
        /// </remarks>
        public static Matrix CreateScalingMatrix(double xScale, double yScale, Point centerPoint)
        {
            double tx = centerPoint.X * (1 - xScale);
            double ty = centerPoint.Y * (1 - yScale);

            // [  xScale  0      ]
            // [  0       yScale ]
            // [  tx      ty     ]
            return new Matrix(m11: xScale, m12: 0d,
                              m21: 0d, m22: yScale,
                              offsetX: tx, offsetY: ty);
        }
    }
}
