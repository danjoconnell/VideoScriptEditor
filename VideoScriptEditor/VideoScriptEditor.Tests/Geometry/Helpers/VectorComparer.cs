/*
    Based on code from Gu.Wpf.Geometry, located at https://github.com/GuOrg/Gu.Wpf.Geometry
    Gu.Wpf.Geometry is licensed under the MIT license (MIT), Copyright (c) 2015 Johan Larsson.
    The full text of the license is available at https://github.com/GuOrg/Gu.Wpf.Geometry/blob/master/LICENSE.md
*/

using System.Collections.Generic;
using System.Windows;

namespace VideoScriptEditor.Geometry.Tests
{
    public sealed class VectorComparer : IEqualityComparer<Vector>
    {
        public static readonly VectorComparer TwoDigits = new VectorComparer(2);

        private readonly int digits;

        private VectorComparer(int digits)
        {
            this.digits = digits;
        }

        public static bool Equals(Vector x, Vector y, int decimalDigits)
        {
            return x.Round(decimalDigits) == y.Round(decimalDigits);
        }

        public bool Equals(Vector x, Vector y)
        {
            return Equals(x, y, this.digits);
        }

        public int GetHashCode(Vector obj)
        {
            return obj.Round(this.digits).GetHashCode();
        }
    }
}
