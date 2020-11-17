using System;
using System.ComponentModel;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Services.ScriptVideo;

namespace VideoScriptEditor.ViewModels.Timeline
{
    /// <summary>
    /// Interface abstracting a view model that encapsulates presentation logic for a Video Timeline Track view.
    /// </summary>
    public interface IVideoTimelineTrackViewModel : INotifyPropertyChanged, IComparable<IVideoTimelineTrackViewModel>
    {
        /// <summary>
        /// The zero-based timeline track number.
        /// </summary>
        int TrackNumber { get; set; }

        /// <summary>
        /// Gets a sorted collection of the <see cref="SegmentViewModelBase">segments</see> on the track.
        /// </summary>
        SegmentViewModelCollection TrackSegments { get; }

        /// <summary>
        /// Gets the runtime context of the <see cref="IScriptVideoService"/> instance.
        /// </summary>
        IScriptVideoContext ScriptVideoContext { get; }
    }
}