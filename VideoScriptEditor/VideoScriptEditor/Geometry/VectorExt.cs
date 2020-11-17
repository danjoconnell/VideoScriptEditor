/*
    Based on code from Gu.Wpf.Geometry, located at https://github.com/GuOrg/Gu.Wpf.Geometry
    Gu.Wpf.Geometry is licensed under the MIT license (MIT), Copyright (c) 2015 Johan Larsson.
    The full text of the license is available at https://github.com/GuOrg/Gu.Wpf.Geometry/blob/master/LICENSE.md
*/
namespace VideoScriptEditor.Geometry
{
    using System;
    using System.Globalization;
    using System.Windows;

    public static class VectorExt
    {
        public static Quadrant Quadrant(this Vector v)
        {
            if (v.X >= 0)
            {
                return v.Y >= 0
                           ? Geometry.Quadrant.PositiveXPositiveY
                           : Geometry.Quadrant.PositiveXNegativeY;
            }

            return v.Y >= 0
                       ? Geometry.Quadrant.NegativeXPositiveY
                       : Geometry.Quadrant.NegativeXNegativeY;
        }

        public static double AngleTo(this Vector v, Vector other)
        {
            return Vector.AngleBetween(v, other);
        }

        public static Vector Rotate(this Vector v, double degrees)
        {
            return v.RotateRadians(degrees * Constants.DegToRad);
        }

        public static double AngleToPositiveX(this Vector v)
        {
            var angle = Math.Atan2(v.Y, v.X) * Constants.RadToDeg;
            return angle;
        }

        public static Vector? SnapToOrtho(this Vector v)
        {
            var angle = v.AngleToPositiveX();
            if (angle > -135 && angle < -45)
            {
                return new Vector(0, -v.Length);
            }

            if (angle > -45 && angle < 45)
            {
                return new Vector(v.Length, 0);
            }

            if (angle > 45 && angle < 135)
            {
                return new Vector(0, v.Length);
            }

            if ((angle >= -180 && angle < -135) || (angle > 135 && angle <= 180))
            {
                return new Vector(-v.Length, 0);
            }

            return null;
        }

        public static double DotProduct(this Vector v, Vector other)
        {
            return Vector.Multiply(v, other);
        }

        public static Vector ProjectOn(this Vector v, Vector other)
        {
            var dp = v.DotProduct(other);
            return dp * other;
        }

        public static Vector RotateRadians(this Vector v, double radians)
        {
            var ca = Math.Cos(radians);
            var sa = Math.Sin(radians);
            return new Vector(ca * v.X - sa * v.Y, sa * v.X + ca * v.Y);
        }

        public static Vector Normalized(this Vector v)
        {
            var uv = new Vector(v.X, v.Y);
            uv.Normalize();
            return uv;
        }

        public static Vector Negated(this Vector v)
        {
            var negated = new Vector(v.X, v.Y);
            negated.Negate();
            return negated;
        }

        public static Vector Round(this Vector v, int digits = 0)
        {
            return new Vector(Math.Round(v.X, digits), Math.Round(v.Y, digits));
        }

        public static string ToString(this Vector? self, string format = "F1")
        {
            return self == null ? "null" : self.Value.ToString(format);
        }

        public static string ToString(this Vector self, string format = "F1")
        {
            return $"{self.X.ToString(format, CultureInfo.InvariantCulture)},{self.Y.ToString(format, CultureInfo.InvariantCulture)}";
        }
    }
}
