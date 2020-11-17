using System.Windows;
using Xunit;

namespace VideoScriptEditor.Geometry.Tests
{
    public class PointExtensionsTests
    {
        public static TheoryData<Point, Point, double> AngleToTestData =>
            new TheoryData<Point, Point, double>
            {
                { new Point(0, 1), new Point(1, 0), -45d },
                { new Point(0, 479), new Point(1, 480), 45d }
            };

        [Theory]
        [MemberData(nameof(AngleToTestData))]
        public void AngleToTest(Point fromPoint, Point toPoint, double expectedResult)
        {
            Assert.Equal(expectedResult, fromPoint.AngleTo(toPoint));
        }
    }
}