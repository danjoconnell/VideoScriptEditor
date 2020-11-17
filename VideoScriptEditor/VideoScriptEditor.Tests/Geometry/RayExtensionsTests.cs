using System.Windows;
using Xunit;

namespace VideoScriptEditor.Geometry.Tests
{
    public class RayExtensionsTests
    {
        [Fact]
        public void IntersectWithTest()
        {
            Ray topToBottom = new Ray(new Point(20, 20), new Vector(0, 1));
            Ray rightToLeft = new Ray(new Point(640, 480), new Vector(-1, 0));

            Point expected = new Point(20, 480);
            Point? actual = topToBottom.IntersectWith(rightToLeft);
            Assert.Equal(expected, actual);
        }
    }
}