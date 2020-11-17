#pragma once

namespace VideoScriptEditor::PreviewRenderer::Unmanaged
{
    /// <summary>
    /// Encapsulates video information from a loaded script.
    /// </summary>
    struct LoadedScriptVideoInfo
    {
        /// <summary>
        /// Whether the loaded script contains renderable video.
        /// </summary>
        bool HasVideo;

        /// <summary>
        /// The width of the video in pixels.
        /// </summary>
        int PixelWidth;

        /// <summary>
        /// The height of the video in pixels.
        /// </summary>
        int PixelHeight;

        /// <summary>
        /// The total number of video frames contained in the video.
        /// </summary>
        int FrameCount;

        /// <summary>
        /// The video frame rate numerator.
        /// </summary>
        unsigned int FpsNumerator;

        /// <summary>
        /// The video frame rate denominator.
        /// </summary>
        unsigned int FpsDenominator;
    };

    /// <summary>
    /// Specifies the method for performing video resize.
    /// </summary>
    enum class VideoSizeMode
    {
        /// <summary>
        /// No resize, the original video width and height are retained.
        /// </summary>
        None,

        /// <summary>
        /// Letterbox to size.
        /// </summary>
        Letterbox
    };

    /// <summary>
    /// Encapsulates video resizing information such as resize mode, pixel width and height.
    /// </summary>
    struct VideoSizeInfo
    {
        /// <summary>
        /// The video resize method.
        /// </summary>
        VideoSizeMode SizeMode;

        /// <summary>
        /// The desired width of the video in pixels.
        /// </summary>
        int Width;

        /// <summary>
        /// The desired height of the video in pixels.
        /// </summary>
        int Height;
    };
}