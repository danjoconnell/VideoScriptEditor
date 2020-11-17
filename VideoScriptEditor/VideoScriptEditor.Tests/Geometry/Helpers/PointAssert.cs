/*
    Based on code from Gu.Wpf.Geometry, located at https://github.com/GuOrg/Gu.Wpf.Geometry
    Gu.Wpf.Geometry is licensed under the MIT license (MIT), Copyright (c) 2015 Johan Larsson.
    The full text of the license is available at https://github.com/GuOrg/Gu.Wpf.Geometry/blob/master/LICENSE.md
*/

using System.Windows;
using Xunit.Sdk;

namespace VideoScriptEditor.Geometry.Tests
{
    public static class PointAssert
    {
        public static void Equal(Point expected, Point actual, int digits = 2)
        {
            if (!PointComparer.Equals(expected, actual, digits))
            {
                throw new EqualException(expected, actual);
            }
        }

        public static void Equal(Point? expected, Point? actual, int digits = 2)
        {
            if (expected == null)
            {
                if (actual == null)
                {
                    return;
                }

                throw new EqualException(expected, actual);
            }

            if (actual == null)
            {
                throw new EqualException(expected, actual);
            }

            Equal(expected.Value, actual.Value, digits);
        }
    }
}
