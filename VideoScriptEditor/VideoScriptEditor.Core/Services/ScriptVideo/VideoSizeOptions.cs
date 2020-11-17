using System;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Primitives;

namespace VideoScriptEditor.Services.ScriptVideo
{
    /// <summary>
    /// Encapsulates video resizing options such as resize mode, aspect ratio or pixel width and height.
    /// </summary>
    public struct VideoSizeOptions : IEquatable<VideoSizeOptions>
    {
        /// <summary>
        /// Specifies the video resize method.
        /// </summary>
        public VideoResizeMode ResizeMode;

        /// <summary>
        /// The desired aspect ratio if the video is to be resized using an aspect ratio.
        /// </summary>
        public Ratio? AspectRatio;

        /// <summary>
        /// The desired width of the video in pixels.
        /// </summary>
        public int PixelWidth;

        /// <summary>
        /// The desired height of the video in pixels.
        /// </summary>
        public int PixelHeight;

        /// <inheritdoc/>
        public bool Equals(VideoSizeOptions other)
        {
            return ResizeMode == other.ResizeMode
                   && AspectRatio == other.AspectRatio
                   && PixelWidth == other.PixelWidth
                   && PixelHeight == other.PixelHeight;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is VideoSizeOptions otherVideoSizeOptions && Equals(otherVideoSizeOptions);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(ResizeMode, AspectRatio, PixelWidth, PixelHeight);
        }

        /// <summary>
        /// Compares two <see cref="VideoSizeOptions"/> instances for exact equality.
        /// </summary>
        /// <param name="left">The left hand side <see cref="VideoSizeOptions"/> instance to compare.</param>
        /// <param name="right">The right hand side <see cref="VideoSizeOptions"/> instance to compare.</param>
        /// <returns>True if the two <see cref="VideoSizeOptions"/> instances are exactly equal, False otherwise.</returns>
        public static bool operator ==(VideoSizeOptions left, VideoSizeOptions right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="VideoSizeOptions"/> instances for exact inequality.
        /// </summary>
        /// <param name="left">The left hand side <see cref="VideoSizeOptions"/> instance to compare.</param>
        /// <param name="right">The right hand side <see cref="VideoSizeOptions"/> instance to compare.</param>
        /// <returns>True if the two <see cref="VideoSizeOptions"/> instances are exactly unequal, False otherwise.</returns>
        public static bool operator !=(VideoSizeOptions left, VideoSizeOptions right)
        {
            return !(left == right);
        }
    }
}
