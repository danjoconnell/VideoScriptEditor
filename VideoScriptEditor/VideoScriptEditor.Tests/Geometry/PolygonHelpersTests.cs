using System.Windows;
using Xunit;

namespace VideoScriptEditor.Geometry.Tests
{
    public class PolygonHelpersTests
    {
        [Fact]
        public void GetBoundsTest()
        {
            Point[] trianglePoints = new Point[]
            {
                new Point(491,198),
                new Point(402,316),
                new Point(580,316)
            };

            Rect expected = new Rect(402, 198, 178, 118);
            Rect actual = PolygonHelpers.GetBounds(trianglePoints);
            Assert.Equal(expected, actual);
        }
    }
}