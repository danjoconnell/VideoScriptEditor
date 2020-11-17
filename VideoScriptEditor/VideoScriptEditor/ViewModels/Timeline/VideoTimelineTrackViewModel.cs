using MonitoredUndo;
using Prism.Mvvm;
using System;
using System.Diagnostics.CodeAnalysis;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Services.ScriptVideo;

namespace VideoScriptEditor.ViewModels.Timeline
{
    /// <summary>
    /// View Model encapsulating presentation logic for a Video Timeline Track view.
    /// </summary>
    /// <remarks>Implementation of <see cref="IVideoTimelineTrackViewModel"/>.</remarks>
    public class VideoTimelineTrackViewModel : BindableBase, IVideoTimelineTrackViewModel, ISupportsUndo
    {
        private readonly IChangeFactory _undoChangeFactory;
        private readonly object _rootUndoObject;

        private int _trackNumber;

        /// <inheritdoc cref="IVideoTimelineTrackViewModel.TrackNumber"/>
        public int TrackNumber
        {
            get => _trackNumber;
            set
            {
                if (_trackNumber != value)
                {
                    _undoChangeFactory.OnChanging(this, nameof(TrackNumber), _trackNumber, value);
                    SetProperty(ref _trackNumber, value);
                }
            }
        }

        /// <inheritdoc cref="IVideoTimelineTrackViewModel.TrackSegments"/>
        public SegmentViewModelCollection TrackSegments { get; }

        /// <inheritdoc cref="IVideoTimelineTrackViewModel.ScriptVideoContext"/>
        public IScriptVideoContext ScriptVideoContext { get; }

        /// <summary>
        /// Creates a new <see cref="VideoTimelineTrackViewModel"/> instance.
        /// </summary>
        /// <param name="trackNumber">The zero-based timeline track number.</param>
        /// <param name="rootUndoObject">The undo "root document" or "root object" for this view model.</param>
        /// <param name="undoChangeFactory">The <see cref="IChangeFactory"/> instance for undo <see cref="Change"/> creation.</param>
        /// <param name="scriptVideoContext">The runtime context of the <see cref="IScriptVideoService"/> instance.</param>
        public VideoTimelineTrackViewModel(int trackNumber, object rootUndoObject, IChangeFactory undoChangeFactory, IScriptVideoContext scriptVideoContext)
        {
            _trackNumber = trackNumber;

            _rootUndoObject = rootUndoObject;
            _undoChangeFactory = undoChangeFactory;

            ScriptVideoContext = scriptVideoContext;

            TrackSegments = new SegmentViewModelCollection();
        }

        /// <inheritdoc cref="ISupportsUndo.GetUndoRoot"/>
        public object GetUndoRoot() => _rootUndoObject;

        /// <inheritdoc cref="IComparable{T}.CompareTo(T)"/>
        public int CompareTo([AllowNull] IVideoTimelineTrackViewModel other)
        {
            // If other is not a valid object reference, this instance is greater. 
            if (other == null) return 1;

            return TrackNumber.CompareTo(other.TrackNumber);
        }
    }
}
