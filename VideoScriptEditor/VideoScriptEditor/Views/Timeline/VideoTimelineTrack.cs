using System.Windows.Controls;
using VideoScriptEditor.ViewModels.Timeline;

namespace VideoScriptEditor.Views.Timeline
{
    /// <summary>
    /// The Video Timeline Track view.
    /// </summary>
    public class VideoTimelineTrack : Canvas
    {
        private IVideoTimelineTrackViewModel ViewModel => DataContext as IVideoTimelineTrackViewModel;

        /// <inheritdoc cref="IVideoTimelineTrackViewModel.TrackNumber"/>
        public int TrackNumber => ViewModel.TrackNumber;

        /// <summary>
        /// The calculated pixel width of each frame in the track.
        /// </summary>
        public double PixelsPerFrame => ActualWidth / ViewModel.ScriptVideoContext.SeekableVideoFrameCount;

        /// <summary>
        /// Creates a new <see cref="VideoTimelineTrack"/> element.
        /// </summary>
        public VideoTimelineTrack() : base()
        {
        }
    }
}
