using System.Windows;
using Xunit;

namespace VideoScriptEditor.Geometry.Tests
{
    public class RectExtensionsTests
    {
        [Fact]
        public void TopCenterTest()
        {
            Rect testRect = new Rect(10, 10, 20, 20);
            
            Point expectedTopCenter = new Point(20, 10);
            Assert.Equal(expectedTopCenter, testRect.TopCenter());
        }
    }
}