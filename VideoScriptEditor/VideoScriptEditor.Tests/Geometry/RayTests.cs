/*
    Based on code from Gu.Wpf.Geometry, located at https://github.com/GuOrg/Gu.Wpf.Geometry
    Gu.Wpf.Geometry is licensed under the MIT license (MIT), Copyright (c) 2015 Johan Larsson.
    The full text of the license is available at https://github.com/GuOrg/Gu.Wpf.Geometry/blob/master/LICENSE.md
*/

using System;
using System.Diagnostics;
using System.Windows;
using Xunit;

namespace VideoScriptEditor.Geometry.Tests
{
    public class RayTests
    {
        [Theory]
        [InlineData("-2,0; 1,0", "0,0; 1; 1", "-1,0")]
        [InlineData("0,0; 1,0", "0,0; 1; 1", "1,0")]
        [InlineData("0,0; 1,0", "0,0; 3; 5", "3,0")]
        [InlineData("0,0; -1,0", "0,0; 3; 5", "-3,0")]
        [InlineData("0,0; -1,0", "0,0; 1; 1", "-1,0")]
        [InlineData("-2,1; 1,0", "0,0; 1; 1", "0,1")]
        [InlineData("2,0; -1,0", "0,0; 1; 1", "1,0")]
        [InlineData("0,2; 0,-1", "0,0; 1; 1", "0,1")]
        [InlineData("0,-2; 0,1", "0,0; 1; 1", "0,-1")]
        [InlineData("0,0; 0,1", "0,0; 1; 1", "0,1")]
        [InlineData("0,0; 0,1", "0,0; 2; 3", "0,3")]
        [InlineData("0,0; 0,-1", "0,0; 1; 1", "0,-1")]
        [InlineData("0,0; 0,-1", "0,0; 2; 3", "0,-3")]
        [InlineData("-5,8; 1,-1", "1,2; 3; 4", "-1.4,4.4")] // got this from CAD
        [InlineData("-2,1; 1,0", "0,1; 1; 1", "-1,1")]
        [InlineData("0,3; 0,-1", "0,1; 1; 1", "0,2")]
        [InlineData("0,-1; 0,1", "0,1; 1; 1", "0,0")]
        [InlineData("-2,0; -1,0", "0,0; 1; 1", "null")]
        [InlineData("-2,2; 1,0", "0,0; 1; 1", "null")]
        public void FirstIntersectionWithEllipse(string rs, string es, string eps)
        {
            var ray = Ray.Parse(rs);
            var ellipse = Ellipse.Parse(es);
            var expected = eps == "null" ? (Point?)null : Point.Parse(eps);
            var actual = ray.FirstIntersectionWith(ellipse);
            PointAssert.Equal(expected, actual, 2);
        }

        [Theory]
        [InlineData("0,0; 1; 1")]
        [InlineData("0,0; 2; 3")]
        [InlineData("1,2; 3; 4")]
        [InlineData("-1,-2; 3; 4")]
        public void FirstIntersectionWithEllipseFromInsideRoundtrips(string es)
        {
            var ellipse = Ellipse.Parse(es);
            var xv = new Vector(1, 0);
            for (var i = -180; i < 180; i++)
            {
                var direction = xv.Rotate(i);
                var expected = ellipse.PointOnCircumference(direction);
                var ray = new Ray(ellipse.CenterPoint, direction);
                var actual = ray.FirstIntersectionWith(ellipse);
                PointAssert.Equal(expected, actual, 2);
            }
        }

        [Theory]
        [InlineData("0,0; 1; 1")]
        [InlineData("1,2; 3; 4")]
        [InlineData("-1,-2; 3; 4")]
        public void FirstIntersectionWithEllipseFromOutsideRoundtrips(string es)
        {
            var ellipse = Ellipse.Parse(es);
            var xv = new Vector(1, 0);
            for (var i = -180; i < 180; i++)
            {
                var fromCenterDirection = xv.Rotate(i);
                var pointOnCircumference = ellipse.PointOnCircumference(fromCenterDirection);
                var ray = new Ray(pointOnCircumference + fromCenterDirection, fromCenterDirection.Negated());
                var actual = ray.FirstIntersectionWith(ellipse);
                PointAssert.Equal(pointOnCircumference, actual, 2);
                for (var j = -70; j < 70; j++)
                {
                    var direction = fromCenterDirection.Rotate(j);
                    ray = new Ray(pointOnCircumference + direction, direction.Negated());
                    actual = ray.FirstIntersectionWith(ellipse);
                    PointAssert.Equal(pointOnCircumference, actual, 2);
                }
            }
        }

        [Theory]
        [InlineData("0 0 1 1")]
        [InlineData("1 2 3 4")]
        public void FirstIntersectionWithRectFromOutsideRoundtrips(string rs)
        {
            var rect = Rect.Parse(rs);
            var xAxis = new Vector(1, 0);
            for (var i = -180; i < 180; i++)
            {
                var direction = xAxis.Rotate(i);
                var fromCenter = new Ray(rect.CenterPoint(), direction);
                var pointOnRect = fromCenter.FirstIntersectionWith(rect).GetValueOrDefault();
                var ray = new Ray(pointOnRect + direction, direction.Negated());
                var actual = ray.FirstIntersectionWith(rect);
                PointAssert.Equal(pointOnRect, actual, 2);

                if (rect.ClosestCornerPoint(pointOnRect)
                        .DistanceTo(pointOnRect) < 0.01)
                {
                    continue;
                }

                Vector wallNormal;
                if (Math.Abs(pointOnRect.X - rect.Left) < Constants.Tolerance)
                {
                    wallNormal = new Vector(-1, 0);
                }
                else if (Math.Abs(pointOnRect.X - rect.Right) < Constants.Tolerance)
                {
                    wallNormal = new Vector(1, 0);
                }
                else if (Math.Abs(pointOnRect.Y - rect.Bottom) < Constants.Tolerance)
                {
                    wallNormal = new Vector(0, 1);
                }
                else
                {
                    wallNormal = new Vector(0, -1);
                }

                for (var j = -89; j < 89; j++)
                {
                    var rayDirection = wallNormal.Rotate(j);
                    ray = new Ray(pointOnRect + rayDirection, rayDirection.Negated());
                    actual = ray.FirstIntersectionWith(rect);
                    if (!NullablePointComparer.TwoDigits.Equals(pointOnRect, actual))
                    {
                        Debugger.Break();
                    }

                    PointAssert.Equal(pointOnRect, actual, 2);
                }
            }
        }
    }
}
