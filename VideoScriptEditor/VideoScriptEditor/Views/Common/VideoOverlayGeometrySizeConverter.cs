using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using VideoScriptEditor.Geometry;
using Debug = System.Diagnostics.Debug;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.Views.Common
{
    /// <summary>
    /// Converter for calculating the scaled size and/or location of a geometric item in a Video Overlay element
    /// relative to the video frame size.
    /// </summary>
    /// <remarks>
    /// Used for sizing and positioning geometric items according to the video frame ratio
    /// whenever the parent Video Overlay element is resized.
    /// <para>
    /// Expects four bound values; the value to be scaled, the video frame <see cref="System.Drawing.Size"/>,
    /// the double-precision Video Overlay element width and the double-precision Video Overlay element height.
    /// </para>
    /// </remarks>
    public class VideoOverlayGeometrySizeConverter : IMultiValueConverter
    {
        /// <summary>
        /// Calculates the scaled size and/or location of a geometric item in a Video Overlay element
        /// relative to the video frame size.
        /// </summary>
        /// <remarks>
        /// Expects four bound values; the value to be scaled, the video frame <see cref="System.Drawing.Size"/>,
        /// the double-precision Video Overlay element width and the double-precision Video Overlay element height.
        /// </remarks>
        /// <inheritdoc cref="IMultiValueConverter.Convert(object[], Type, object, CultureInfo)"/>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 4 && values[1] is SizeI videoFrameSize && values[2] is double videoOverlayElementWidth && values[3] is double videoOverlayElementHeight)
            {
                double xScaleFactor = videoOverlayElementWidth / videoFrameSize.Width;
                double yScaleFactor = videoOverlayElementHeight / videoFrameSize.Height;

                switch (values[0])
                {
                    case Rect rect:
                        rect.Scale(xScaleFactor, yScaleFactor);
                        return rect;

                    case Point point:
                        return point.Scale(xScaleFactor, yScaleFactor);

                    case double doubleVal when parameter is Axis axis:
                        if (axis == Axis.X)
                        {
                            return doubleVal * xScaleFactor;
                        }
                        else if (axis == Axis.Y)
                        {
                            return doubleVal * yScaleFactor;
                        }
                        break;
                }
            }

            Debug.Fail("Converter did not produce a value.");
            return DependencyProperty.UnsetValue;
        }

        /// <summary>Not supported. Don't call this method.</summary>
        /// <returns>Throws a <see cref="NotSupportedException"/></returns>
        /// <inheritdoc cref="IMultiValueConverter.ConvertBack(object, Type[], object, CultureInfo)"/>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
