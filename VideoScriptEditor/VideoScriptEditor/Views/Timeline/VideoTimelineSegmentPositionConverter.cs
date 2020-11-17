using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace VideoScriptEditor.Views.Timeline
{
    /// <summary>
    /// Converter for calculating the <see cref="FrameworkElement.Width"/> or <see cref="Canvas.LeftProperty"/>
    /// property value for a <see cref="VideoTimelineSegment"/> element in a <see cref="VideoTimelineView"/>.
    /// </summary>
    public class VideoTimelineSegmentPositionConverter : IMultiValueConverter
    {
        /// <summary>
        /// Calculates the <see cref="FrameworkElement.Width"/> or <see cref="Canvas.LeftProperty"/> property value
        /// for a <see cref="VideoTimelineSegment"/> element depending on the property name specified by
        /// the <paramref name="parameter"/>.
        /// </summary>
        /// <inheritdoc cref="IMultiValueConverter.Convert(object[], Type, object, CultureInfo)"/>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 3 && parameter is string elementPropertyName
                && values[0] is double timelineElementWidth
                && values[1] is int seekableVideoFrameCount && seekableVideoFrameCount > 0)
            {
                double pixelsPerFrame = timelineElementWidth / seekableVideoFrameCount;

                if (elementPropertyName == nameof(VideoTimelineSegment.Width) && values[2] is int segmentFrameDuration)
                {
                    return pixelsPerFrame * segmentFrameDuration;
                }
                else if (elementPropertyName == Canvas.LeftProperty.Name && values[2] is int segmentStartFrame)
                {
                    return pixelsPerFrame * segmentStartFrame;
                }
            }

            return Binding.DoNothing;
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
