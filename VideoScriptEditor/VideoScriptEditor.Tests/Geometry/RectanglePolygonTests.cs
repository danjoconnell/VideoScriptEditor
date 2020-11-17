using System.Windows;
using System.Windows.Media;
using Xunit;

namespace VideoScriptEditor.Geometry.Tests
{
    public class RectanglePolygonTests
    {
        [Fact]
        public void Axis_Aligned_Has_Zero_Angle_Test()
        {
            RectanglePolygon testRect = new RectanglePolygon(axisAlignedRect: new Rect(0, 0, 640, 480), rotationAngle: 0);
            Assert.Equal(0d, testRect.Angle);
        }

        [Fact]
        public void RotationMatrixTest()
        {
            RectanglePolygon testRect = new RectanglePolygon(new Point(100, 100), new Point(300, 100), new Point(300, 200), new Point(100, 200));
            double expectedAngle = 0d;
            Assert.Equal(expectedAngle, testRect.Angle);

            Matrix matrix = MatrixFactory.CreateRotationMatrix(90, testRect.Center);
            Point[] rectPoints = new[] { testRect.TopLeft, testRect.TopRight, testRect.BottomRight, testRect.BottomLeft };
            matrix.Transform(rectPoints);
            testRect = new RectanglePolygon(rectPoints[0], rectPoints[1], rectPoints[2], rectPoints[3]);
            expectedAngle = 90d;
            Assert.Equal(expectedAngle, testRect.Angle);

            matrix = MatrixFactory.CreateRotationMatrix(-45, testRect.Center);
            rectPoints = new[] { testRect.TopLeft, testRect.TopRight, testRect.BottomRight, testRect.BottomLeft };
            matrix.Transform(rectPoints);
            testRect = new RectanglePolygon(rectPoints[0], rectPoints[1], rectPoints[2], rectPoints[3]);
            expectedAngle = 45d;
            Assert.Equal(expectedAngle, testRect.Angle);
        }
    }
}
