using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using VideoScriptEditor.ViewModels.Timeline;

namespace VideoScriptEditor.Views.Timeline
{
    /// <summary>
    /// Converter for calculating the <see cref="FrameworkElement.Width"/> of a scrollable timeline
    /// element for a given <see cref="IVideoTimelineViewModel.ZoomLevel"/> value.
    /// </summary>
    public class ZoomLevelToTimelineWidthConverter : IMultiValueConverter
    {
        /// <summary>
        /// Calculates the <see cref="FrameworkElement.Width"/> of a scrollable timeline
        /// element for a given <see cref="IVideoTimelineViewModel.ZoomLevel"/> value.
        /// </summary>
        /// <inheritdoc cref="IMultiValueConverter.Convert(object[], Type, object, CultureInfo)"/>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 4
                && values[0] is double zoomLevel
                && values[1] is int totalFrames
                && values[2] is double frameWidth
                && values[3] is double minimumWidth)
            {
                double fullyZoomedWidth = frameWidth * totalFrames;
                double zoomPercentageMultiplier = zoomLevel / 100d;
                double zoomedWidth = fullyZoomedWidth * zoomPercentageMultiplier;

                // Decreasing the width of the element below its minimum width could cause layout issues.
                return Math.Max(minimumWidth, zoomedWidth);
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
