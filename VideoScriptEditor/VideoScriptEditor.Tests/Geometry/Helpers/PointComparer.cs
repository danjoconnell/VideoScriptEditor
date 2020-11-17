/*
    Based on code from Gu.Wpf.Geometry, located at https://github.com/GuOrg/Gu.Wpf.Geometry
    Gu.Wpf.Geometry is licensed under the MIT license (MIT), Copyright (c) 2015 Johan Larsson.
    The full text of the license is available at https://github.com/GuOrg/Gu.Wpf.Geometry/blob/master/LICENSE.md
*/

using System.Collections.Generic;
using System.Windows;

namespace VideoScriptEditor.Geometry.Tests
{
    public sealed class PointComparer : IEqualityComparer<Point>
    {
        public static readonly PointComparer TwoDigits = new PointComparer(2);

        private readonly int digits;

        public PointComparer(int digits)
        {
            this.digits = digits;
        }

        public static bool Equals(Point x, Point y, int decimalDigits)
        {
            return x.Round(decimalDigits) == y.Round(decimalDigits);
        }

        public bool Equals(Point x, Point y)
        {
            return Equals(x, y, this.digits);
        }

        public int GetHashCode(Point obj)
        {
            return obj.Round(this.digits).GetHashCode();
        }
    }
}
