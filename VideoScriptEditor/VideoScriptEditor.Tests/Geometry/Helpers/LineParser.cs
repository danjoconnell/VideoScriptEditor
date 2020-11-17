/*
    Based on code from Gu.Wpf.Geometry, located at https://github.com/GuOrg/Gu.Wpf.Geometry
    Gu.Wpf.Geometry is licensed under the MIT license (MIT), Copyright (c) 2015 Johan Larsson.
    The full text of the license is available at https://github.com/GuOrg/Gu.Wpf.Geometry/blob/master/LICENSE.md
*/

using System;
using System.Windows;

namespace VideoScriptEditor.Geometry.Tests
{
    internal static class LineParser
    {
        internal static Line AsLine(this string s)
        {
            var strings = s.Split(';');
            if (strings.Length != 2)
            {
                throw new FormatException("Could not parse line.");
            }

            var sp = Point.Parse(strings[0]);
            var ep = Point.Parse(strings[1]);
            return new Line(sp, ep);
        }
    }
}
