using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using VideoScriptEditor.ViewModels.Timeline;

namespace VideoScriptEditor.Views.Timeline
{
    /// <summary>
    /// Converter for calculating the <see cref="Canvas.LeftProperty"/> and <see cref="Canvas.TopProperty"/>
    /// positions for a key frame element. 
    /// </summary>
    public class VideoTimelineKeyFramePositionConverter : IMultiValueConverter
    {
        /// <summary>
        /// Calculates the key frame element position for the <see cref="Canvas"/>
        /// attached property name specified by the <paramref name="parameter"/>.
        /// </summary>
        /// <remarks>Either <see cref="Canvas.LeftProperty"/> or <see cref="Canvas.TopProperty"/>.</remarks>
        /// <inheritdoc cref="IMultiValueConverter.Convert(object[], Type, object, CultureInfo)"/>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string canvasPropertyName)
            {
                if (canvasPropertyName == Canvas.LeftProperty.Name && values.Length == 5
                    && values[0] is double segmentElementWidth
                    && values[1] is int segmentStartFrame
                    && values[2] is int segmentEndFrame
                    && values[3] is double keyFrameElementWidth
                    && values[4] is int keyFrameNumber)
                {
                    return CalculateLeftPosition(segmentElementWidth, segmentStartFrame, segmentEndFrame,
                                                 keyFrameElementWidth, keyFrameNumber);
                }
                else if (canvasPropertyName == Canvas.TopProperty.Name && values.Length == 2
                         && values[0] is double segmentElementHeight
                         && values[1] is double keyFrameElementHeight)
                {
                    // Center vertically
                    return (segmentElementHeight / 2d) - (keyFrameElementHeight / 2d);
                }
            }

            return Binding.DoNothing;
        }

        /// <summary>
        /// Calculates the <see cref="Canvas.LeftProperty"/> position for a key frame element.
        /// </summary>
        /// <param name="segmentElementWidth">The width of the segment element.</param>
        /// <param name="segmentStartFrame">The <see cref="SegmentViewModelBase.StartFrame"/>.</param>
        /// <param name="segmentEndFrame">The <see cref="SegmentViewModelBase.EndFrame"/>.</param>
        /// <param name="keyFrameElementWidth">The width of the key frame element.</param>
        /// <param name="keyFrameNumber">The <see cref="KeyFrameViewModelBase.FrameNumber"/>.</param>
        /// <returns>The value for the key frame element's <see cref="Canvas.LeftProperty"/>.</returns>
        private double CalculateLeftPosition(double segmentElementWidth, int segmentStartFrame,
                                             int segmentEndFrame, double keyFrameElementWidth, int keyFrameNumber)
        {
            int segmentFrameDuration = segmentEndFrame - segmentStartFrame + 1; // Start/End Frames are zero-based and inclusive.
            double pixelsPerFrame = segmentElementWidth / segmentFrameDuration;

            int frameNumberRelativeToSegmentStart = keyFrameNumber - segmentStartFrame;
            double framePixelLeftPosition = pixelsPerFrame * frameNumberRelativeToSegmentStart;

            // Center horizontally on frame pixel position
            return framePixelLeftPosition - (keyFrameElementWidth / 2d);
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
