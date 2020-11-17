/*
    Based on code from Gu.Wpf.Geometry, located at https://github.com/GuOrg/Gu.Wpf.Geometry
    Gu.Wpf.Geometry is licensed under the MIT license (MIT), Copyright (c) 2015 Johan Larsson.
    The full text of the license is available at https://github.com/GuOrg/Gu.Wpf.Geometry/blob/master/LICENSE.md
*/

using System.Windows;
using Xunit;

namespace VideoScriptEditor.Geometry.Tests
{
    public class VectorExtTests
    {
        [Theory]
        [InlineData("1,0", "1,0", "1,0")]
        [InlineData("2,0", "1,0", "2,0")]
        [InlineData("2,3", "1,0", "2,0")]
        [InlineData("2,-3", "1,0", "2,0")]
        [InlineData("-2,-3", "1,0", "-2,0")]
        [InlineData("-1,0", "1,0", "-1,0")]
        [InlineData("0,1", "1,0", "0,0")]
        public void ProjectOn(string v1S, string v2S, string evs)
        {
            var v1 = Vector.Parse(v1S);
            var v2 = Vector.Parse(v2S);
            var expected = Vector.Parse(evs);
            var actual = v1.ProjectOn(v2);
            VectorAssert.Equal(expected, actual, 2);
        }

        [Theory]
        [InlineData("2,0", "2,0")]
        [InlineData("-2,0", "-2,0")]
        [InlineData("0,2", "0,2")]
        [InlineData("0,-2", "0,-2")]
        public void SnapToOrtho(string vs, string evs)
        {
            var v = Vector.Parse(vs);
            var expected = evs == "null" ? (Vector?)null : Vector.Parse(evs);
            var actual = v.SnapToOrtho();
            VectorAssert.Equal(expected, actual, 2);

            var vMinus = v.Rotate(-44);
            actual = vMinus.SnapToOrtho();
            VectorAssert.Equal(expected, actual, 2);

            var vPlus = v.Rotate(44);
            actual = vPlus.SnapToOrtho();
            VectorAssert.Equal(expected, actual, 2);
        }

        [Theory]
        [InlineData("1,0", 90, "0,1")]
        [InlineData("2,0", 90, "0,2")]
        [InlineData("2,3", 90, "-3,2")]
        public void Rotate(string vs, double angle, string evs)
        {
            var vector = Vector.Parse(vs);
            var expected = Vector.Parse(evs);
            var actual = vector.Rotate(angle);
            VectorAssert.Equal(expected, actual, 2);

            var roundtrip = actual.Rotate(-angle);
            VectorAssert.Equal(vector, roundtrip, 2);
        }
    }
}
