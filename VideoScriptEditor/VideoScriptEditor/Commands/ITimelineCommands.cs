using Prism.Commands;
using VideoScriptEditor.Services.ScriptVideo;

namespace VideoScriptEditor.Commands
{
    /// <summary>
    /// Interface abstracting a set of timeline related commands.
    /// </summary>
    public interface ITimelineCommands
    {
        /// <summary>
        /// Command for setting the timeline zoom level percentage.
        /// </summary>
        CompositeCommand SetTimelineZoomLevelCommand { get; }

        /// <summary>
        /// Command for adding a new track to the timeline.
        /// </summary>
        CompositeCommand AddTrackCommand { get; }

        /// <summary>
        /// Command for removing a track from the timeline.
        /// </summary>
        CompositeCommand RemoveTrackCommand { get; }

        /// <summary>
        /// Command for adding a new segment to a timeline track.
        /// </summary>
        CompositeCommand AddTrackSegmentCommand { get; }

        /// <summary>
        /// Encapsulated parameters for the <see cref="AddTrackSegmentCommand"/>.
        /// </summary>
        AddTrackSegmentCommandParameters AddTrackSegmentCommandParameters { get; }

        /// <summary>
        /// Command for removing a segment from a timeline track.
        /// </summary>
        CompositeCommand RemoveTrackSegmentCommand { get; }

        /// <summary>
        /// Command for splitting the selected segment on a timeline track.
        /// </summary>
        CompositeCommand SplitSelectedTrackSegmentCommand { get; }

        /// <summary>
        /// Command for <see cref="IScriptVideoService.SeekFrame(int)">seeking</see> a key frame in a track
        /// located before the current <see cref="IScriptVideoContext.FrameNumber">frame number</see>.
        /// </summary>
        CompositeCommand SeekPreviousKeyFrameInTrackCommand { get; }

        /// <summary>
        /// Command for <see cref="IScriptVideoService.SeekFrame(int)">seeking</see> a key frame in a track
        /// located after the current <see cref="IScriptVideoContext.FrameNumber">frame number</see>.
        /// </summary>
        CompositeCommand SeekNextKeyFrameInTrackCommand { get; }

        /// <summary>
        /// Command for adding a new key frame to a timeline track segment.
        /// </summary>
        CompositeCommand AddTrackSegmentKeyFrameCommand { get; }

        /// <summary>
        /// Command for removing a key frame from a timeline track segment.
        /// </summary>
        CompositeCommand RemoveTrackSegmentKeyFrameCommand { get; }

        /// <summary>
        /// Command for setting key frame data by copying data from the previous key frame in a timeline track segment.
        /// </summary>
        CompositeCommand CopyKeyFrameFromPreviousInTrackSegmentCommand { get; }

        /// <summary>
        /// Command for setting key frame data by copying data from the next key frame in a timeline track segment.
        /// </summary>
        CompositeCommand CopyKeyFrameFromNextInTrackSegmentCommand { get; }

        /// <summary>
        /// Command for copying data from a track segment key frame to the system clipboard.
        /// </summary>
        CompositeCommand CopyTrackSegmentKeyFrameToClipboardCommand { get; }

        /// <summary>
        /// Command for setting track segment key frame data by pasting data from the system clipboard.
        /// </summary>
        CompositeCommand PasteTrackSegmentKeyFrameCommand { get; }
    }
}