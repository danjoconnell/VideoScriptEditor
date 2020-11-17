/*
    Based on code from Gu.Wpf.Geometry, located at https://github.com/GuOrg/Gu.Wpf.Geometry
    Gu.Wpf.Geometry is licensed under the MIT license (MIT), Copyright (c) 2015 Johan Larsson.
    The full text of the license is available at https://github.com/GuOrg/Gu.Wpf.Geometry/blob/master/LICENSE.md
*/

using System.Windows;
using Xunit;

namespace VideoScriptEditor.Geometry.Tests
{
    public class LineTests
    {
        [Theory]
        [InlineData("1,1; 1,2", "0,3; -1,3", "1,1; 1,3")]
        [InlineData("0,0; 1,0", "1,0; 1,1", "0,0; 1,0")]
        [InlineData("0,0; 1,0", "2,0; 2,1", "0,0; 2,0")]
        public void TrimOrExtendEndWith(string l1S, string l2S, string expectedS)
        {
            Line l1 = l1S.AsLine();
            Line l2 = l2S.AsLine();

            Line expected = expectedS.AsLine();
            Line? actual = l1.TrimOrExtendEndWith(l2);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("1,0; 2,0", "0,0; 1,0", "1,0; 2,0")]
        [InlineData("0,0; 1,0", "1,0; 2,0", "0,0; 1,0")]
        public void TrimOrExtendEndWithCollinear(string l1S, string l2S, string expectedS)
        {
            Line l1 = l1S.AsLine();
            Line l2 = l2S.AsLine();

            Line expected = expectedS.AsLine();
            Line? actual = l1.TrimOrExtendEndWith(l2);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("-1,0; 1,0", "0,-1; 0,1", "0,0")]
        [InlineData("-1,0; 1,0", "10,-1; 10,1", "null")]
        [InlineData("-1,0; 0,0", "0,-1; 0,1", "0,0")]
        [InlineData("-1,0; 0,0", "0,-1; 0,0", "0,0")]
        [InlineData("-1,0; 0,0", "0,0; 0,1", "0,0")]
        [InlineData("1,1; 1,2", "0,3; -1,3", "null")]
        public void IntersectionPoint(string l1S, string l2S, string expected)
        {
            Line l1 = l1S.AsLine();
            Line l2 = l2S.AsLine();
            Point? actual = l1.IntersectWith(l2, mustBeBetweenStartAndEnd: true);
            Assert.Equal(expected, actual.ToString("F0"));
            actual = l2.IntersectWith(l1, mustBeBetweenStartAndEnd: true);
            Assert.Equal(expected, actual.ToString("F0"));
        }

        [Theory]
        [InlineData("1,1; -1,-1", "0,0,10,10", "0,0")]
        [InlineData("1,1; -1,1", "0,0,10,10", "0,1")]
        [InlineData("9,1; 11,1", "0,0,10,10", "10,1")]
        [InlineData("1,9; 1,11", "0,0,10,10", "1,10")]
        [InlineData("1,1; 1,-1", "0,0,10,10", "1,0")]
        public void IntersectWithRectangleStartingInside(string ls, string rs, string eps)
        {
            Line l = Line.Parse(ls);
            Rect rect = Rect.Parse(rs);
            Point expected = Point.Parse(eps);
            Point? actual = l.ClosestIntersection(rect);
            PointAssert.Equal(expected, actual, 2);

            actual = l.Flip().ClosestIntersection(rect);
            PointAssert.Equal(expected, actual, 2);

            Line l2 = l.RotateAroundStartPoint(0.01);
            actual = l2.ClosestIntersection(rect);
            PointAssert.Equal(expected, actual, 2);

            Line l3 = l.RotateAroundStartPoint(-0.01);
            actual = l3.ClosestIntersection(rect);
            PointAssert.Equal(expected, actual, 2);
        }

        [Theory]
        [InlineData("1,0; -1,0", "0,0,10,10", "0,0")]
        [InlineData("1,10; -1,10", "0,0,10,10", "0,10")]
        [InlineData("0,-1; 0,1", "0,0,10,10", "0,0")]
        [InlineData("0,11; 0,9", "0,0,10,10", "0,10")]
        [InlineData("1,11; -1,11", "0,0,10,10", "null")]
        public void IntersectWithRectangleWhenTangent(string ls, string rs, string eps)
        {
            Line l = Line.Parse(ls);
            Rect rect = Rect.Parse(rs);
            Point? expected = eps == "null" ? (Point?)null : Point.Parse(eps);
            Point? actual = l.ClosestIntersection(rect);
            PointAssert.Equal(expected, actual, 2);
        }

        [Theory]
        [InlineData("-1,-1; 1,1", "0,0,10,10", "0,0")]
        [InlineData("-1,5; 11,5", "0,0,10,10", "0,5")]
        [InlineData("-1,-2; 11,-2", "0,0,10,10", "null")]
        [InlineData("11,5; -1,5", "0,0,10,10", "10,5")]
        [InlineData("-1,0; 1,0", "0,0,10,10", "0,0")]
        [InlineData("1,-1; 1,11", "0,0,10,10", "1,0")]
        [InlineData("-1,11; 1,9", "0,0,10,10", "0,10")]
        public void IntersectWithRectangleStartingOutside(string ls, string rs, string eps)
        {
            Line l = Line.Parse(ls);
            Rect rect = Rect.Parse(rs);
            Point? expected = eps == "null" ? (Point?)null : Point.Parse(eps);
            Point? actual = l.ClosestIntersection(rect);
            PointAssert.Equal(expected, actual, 2);
        }
    }
}
