/*
    Based on code from Gu.Wpf.Geometry, located at https://github.com/GuOrg/Gu.Wpf.Geometry
    Gu.Wpf.Geometry is licensed under the MIT license (MIT), Copyright (c) 2015 Johan Larsson.
    The full text of the license is available at https://github.com/GuOrg/Gu.Wpf.Geometry/blob/master/LICENSE.md
*/

using System.Windows;
using Xunit;

namespace VideoScriptEditor.Geometry.Tests
{
    public class EllipseTests
    {
        [Theory]
        [InlineData("0,0; 2; 3", "1,0", "2,0")]
        [InlineData("0,0; 2; 3", "0,1", "0,3")]
        [InlineData("0,0; 2; 3", "-1,0", "-2,0")]
        [InlineData("0,0; 2; 3", "0,-1", "0,-3")]
        [InlineData("1,2; 1; 1", "0,1", "1,3")]
        public void PointOnCircumference(string es, string vs, string eps)
        {
            var ellipse = Ellipse.Parse(es);
            var direction = Vector.Parse(vs);
            var expected = Point.Parse(eps);
            var actual = ellipse.PointOnCircumference(direction);
            PointAssert.Equal(expected, actual, 2);
        }

        [Theory]
        [InlineData("0,0; 1; 1", 10, "0.985,0.174")]
        [InlineData("0,0; 2; 3", 0, "2,0")]
        [InlineData("0,0; 2; 3", 90, "0,3")]
        [InlineData("0,0; 2; 3", 10, "1.986,0.35")]
        public void PointAtAngle(string es, double angle, string eps)
        {
            var ellipse = Ellipse.Parse(es);
            var expected = Point.Parse(eps);
            var direction = new Vector(1, 0).Rotate(angle);
            var actual = ellipse.PointOnCircumference(direction);
            PointAssert.Equal(expected, actual, 2);
            Assert.Equal(ellipse.CenterPoint.DistanceTo(actual), ellipse.RadiusInDirection(direction), 3);
        }
    }
}
