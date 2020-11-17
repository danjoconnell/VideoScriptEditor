/*
    Based on code from Gu.Wpf.Geometry, located at https://github.com/GuOrg/Gu.Wpf.Geometry
    Gu.Wpf.Geometry is licensed under the MIT license (MIT), Copyright (c) 2015 Johan Larsson.
    The full text of the license is available at https://github.com/GuOrg/Gu.Wpf.Geometry/blob/master/LICENSE.md
*/

using System.Collections.Generic;
using System.Windows;

namespace VideoScriptEditor.Geometry.Tests
{
    public sealed class NullablePointComparer : IEqualityComparer<Point?>
    {
        public static readonly NullablePointComparer TwoDigits = new NullablePointComparer(2);

        private readonly int digits;

        public NullablePointComparer(int digits)
        {
            this.digits = digits;
        }

        public bool Equals(Point? x, Point? y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return PointComparer.Equals(x.Value, y.Value, this.digits);
        }

        public int GetHashCode(Point? obj)
        {
            return obj?.Round(this.digits).GetHashCode() ?? 0;
        }
    }
}
