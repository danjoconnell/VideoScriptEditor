using System;
using System.ComponentModel;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Primitives;
using Size = System.Drawing.Size;

namespace VideoScriptEditor.Services.ScriptVideo
{
    /// <summary>
    /// An abstract representation of the runtime context of an <see cref="IScriptVideoService"/>.
    /// </summary>
    public interface IScriptVideoContext : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the file path of the AviSynth script
        /// providing source video to the <see cref="IScriptVideoService"/>.
        /// </summary>
        string ScriptFileSource { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ProjectModel"/> providing the <see cref="ScriptFileSource"/>
        /// and editing data for previewing through the <see cref="IScriptVideoService"/>.
        /// </summary>
        /// <remarks>Setting a null value resets the <see cref="IScriptVideoService"/> via the <see cref="IScriptVideoService.CloseScript"/> service method.</remarks>
        ProjectModel Project { get; set; }

        /// <summary>
        /// Gets whether the loaded script (if any) contains renderable video.
        /// </summary>
        bool HasVideo { get; }

        /// <summary>
        /// Gets whether video playback is in progress.
        /// </summary>
        bool IsVideoPlaying { get; }

        /// <summary>
        /// Gets or sets the current zero-based frame number of the video.
        /// </summary>
        int FrameNumber { get; set; }

        /// <summary>
        /// Gets the total number of video frames contained in the video.
        /// </summary>
        int VideoFrameCount { get; }

        /// <summary>
        /// Gets the total number of seekable video frames contained in the video.
        /// Since frame numbering is zero-based, this is one frame less than the <see cref="VideoFrameCount"/>.
        /// </summary>
        int SeekableVideoFrameCount { get; }

        /// <summary>
        /// Gets the original pixel width and height of the video frame.
        /// </summary>
        Size VideoFrameSize { get; }

        /// <summary>
        /// Gets the original aspect ratio of the video in the form of a rational fraction.
        /// </summary>
        Ratio AspectRatio { get; }

        /// <summary>
        /// Gets the approximate video frame rate in the form of a fraction.
        /// </summary>
        Fraction VideoFramerate { get; }

        /// <summary>
        /// Gets the approximate duration of the video.
        /// </summary>
        TimeSpan VideoDuration { get; }

        /// <summary>
        /// Gets or sets the current position of the video as measured in time.
        /// </summary>
        TimeSpan VideoPosition { get; set; }

        /// <summary>
        /// Gets or sets video resizing preview options
        /// such as resize mode, aspect ratio or pixel width and height.
        /// </summary>
        VideoSizeOptions OutputPreviewSize { get; set; }
    }
}
