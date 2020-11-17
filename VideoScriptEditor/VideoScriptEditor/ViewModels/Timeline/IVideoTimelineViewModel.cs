using CodeBits;
using Prism.Commands;
using System;
using System.ComponentModel;
using VideoScriptEditor.Commands;
using VideoScriptEditor.Services.ScriptVideo;

namespace VideoScriptEditor.ViewModels.Timeline
{
    /// <summary>
    /// Interface abstracting a view model that encapsulates presentation logic for the Video Timeline view.
    /// </summary>
    public interface IVideoTimelineViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Command for merging a segment with the segment to its immediate left on a timeline track.
        /// </summary>
        DelegateCommand<SegmentViewModelBase> MergeTrackSegmentLeftCommand { get; }

        /// <summary>
        /// Command for merging a segment with the segment to its immediate right on a timeline track.
        /// </summary>
        DelegateCommand<SegmentViewModelBase> MergeTrackSegmentRightCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.RemoveTrackSegmentCommand"/>
        DelegateCommand<SegmentViewModelBase> RemoveTrackSegmentCommand { get; }

        /// <summary>
        /// Command for renaming a segment in a timeline track.
        /// </summary>
        DelegateCommand<SegmentViewModelBase> RenameTrackSegmentCommand { get; }

        /// <summary>
        /// Gets the runtime context of the <see cref="IScriptVideoService"/> instance.
        /// </summary>
        IScriptVideoContext ScriptVideoContext { get; }

        /// <summary>
        /// Gets or sets a reference to the current view model providing segments to the timeline.
        /// </summary>
        ITimelineSegmentProvidingViewModel TimelineSegmentProvidingViewModel { get; set; }

        /// <summary>
        /// Gets or sets the currently selected track in the timeline.
        /// </summary>
        IVideoTimelineTrackViewModel SelectedTrack { get; set; }

        /// <summary>
        /// Gets a sorted collection of tracks in the timeline.
        /// </summary>
        OrderedObservableCollection<IVideoTimelineTrackViewModel> TimelineTrackCollection { get; }

        /// <summary>
        /// Gets or sets the timeline zoom level percentage.
        /// </summary>
        double ZoomLevel { get; set; }

        /// <summary>
        /// Creates and adds a new segment of a type described by an enumeration value,
        /// with a specific start frame and duration to the given track.
        /// </summary>
        /// <param name="segmentTypeDescriptor">
        /// An <see cref="Enum"/> value describing the type of segment to create and add to the given track.
        /// </param>
        /// <param name="targetTrackNumber">The zero-based track number of the track to add the new segment to.</param>
        /// <param name="targetStartFrameNumber">The zero-based start frame of the new segment.</param>
        /// <param name="segmentFrameDuration">The inclusive frame duration of the new segment.</param>
        void AddTrackSegment(Enum segmentTypeDescriptor, int targetTrackNumber, int targetStartFrameNumber, int segmentFrameDuration);

        /// <summary>
        /// Determines whether a segment with a specific start frame and duration can be added to a given track
        /// without overlapping any existing segments on the track.
        /// </summary>
        /// <param name="targetTrackNumber">The zero-based track number of the track to check.</param>
        /// <param name="targetStartFrameNumber">The desired zero-based start frame of the segment.</param>
        /// <param name="segmentFrameDuration">The inclusive frame duration of the segment.</param>
        /// <returns>True if the segment can be added to the track, otherwise False.</returns>
        bool CanAddTrackSegment(int targetTrackNumber, int targetStartFrameNumber, int segmentFrameDuration);

        /// <summary>
        /// Determines whether the end of the given segment can be expanded or contracted to the specified frame number
        /// without overlapping any existing segments on the track.
        /// </summary>
        /// <param name="trackSegmentViewModel">The track segment to check.</param>
        /// <param name="newEndFrameNumber">The desired new end frame number for the segment.</param>
        /// <returns>
        /// True if the end of the track segment can be expanded or contracted to the specified frame number, otherwise False.
        /// </returns>
        bool CanChangeTrackSegmentEndFrame(SegmentViewModelBase trackSegmentViewModel, int newEndFrameNumber);

        /// <summary>
        /// Determines whether the start of the given segment can be expanded or contracted to the specified frame number
        /// without overlapping any existing segments on the track.
        /// </summary>
        /// <param name="trackSegmentViewModel">The track segment to check.</param>
        /// <param name="newStartFrameNumber">The desired new start frame number for the segment.</param>
        /// <returns>
        /// True if the start of the track segment can be expanded or contracted to the specified frame number, otherwise False.
        /// </returns>
        bool CanChangeTrackSegmentStartFrame(SegmentViewModelBase trackSegmentViewModel, int newStartFrameNumber);

        /// <summary>
        /// Determines whether the given segment can be moved to a different track and/or starting frame position
        /// without overlapping any existing segments on the destination track.
        /// </summary>
        /// <param name="trackSegmentToMove">The track segment to be moved.</param>
        /// <param name="destinationTrackNumber">The zero-based track number of the destination track to check.</param>
        /// <param name="destinationStartFrameNumber">The destination zero-based start frame to check.</param>
        /// <returns>True if the segment can be moved to the destination track and/or starting frame, otherwise False.</returns>
        bool CanMoveTrackSegment(SegmentViewModelBase trackSegmentToMove, int destinationTrackNumber, int destinationStartFrameNumber);

        /// <summary>
        /// Expands or contracts the end of the given track segment to the specified frame number.
        /// </summary>
        /// <param name="trackSegmentViewModel">The track segment to change end frame position.</param>
        /// <param name="newEndFrameNumber">The new end frame number for the track segment.</param>
        void ChangeTrackSegmentEndFrame(SegmentViewModelBase trackSegmentViewModel, int newEndFrameNumber);

        /// <summary>
        /// Expands or contracts the start of the given track segment to the specified frame number.
        /// </summary>
        /// <param name="trackSegmentViewModel">The track segment to change start frame position.</param>
        /// <param name="newStartFrameNumber">The new start frame number for the track segment.</param>
        void ChangeTrackSegmentStartFrame(SegmentViewModelBase trackSegmentViewModel, int newStartFrameNumber);

        /// <summary>
        /// Copies the given segment to a different track and/or starting frame position.
        /// </summary>
        /// <param name="trackSegmentToCopy">The track segment to be copied.</param>
        /// <param name="destinationTrackNumber">The zero-based track number of the destination track.</param>
        /// <param name="destinationStartFrameNumber">The destination zero-based start frame.</param>
        void CopyTrackSegment(SegmentViewModelBase trackSegmentToCopy, int destinationTrackNumber, int destinationStartFrameNumber);

        /// <summary>
        /// Moves the given segment to a different track and/or starting frame position.
        /// </summary>
        /// <param name="trackSegmentToMove">The track segment to be moved.</param>
        /// <param name="destinationTrackNumber">The zero-based track number of the destination track.</param>
        /// <param name="destinationStartFrameNumber">The destination zero-based start frame.</param>
        void MoveTrackSegment(SegmentViewModelBase trackSegmentToMove, int destinationTrackNumber, int destinationStartFrameNumber);
    }
}