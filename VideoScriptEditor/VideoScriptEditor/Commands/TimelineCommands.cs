using Prism.Commands;

namespace VideoScriptEditor.Commands
{
    /// <summary>
    /// Provides a set of timeline related commands.
    /// </summary>
    public class TimelineCommands : ITimelineCommands
    {
        /// <inheritdoc cref="ITimelineCommands.SetTimelineZoomLevelCommand"/>
        public CompositeCommand SetTimelineZoomLevelCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.AddTrackCommand"/>
        public CompositeCommand AddTrackCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.RemoveTrackCommand"/>
        public CompositeCommand RemoveTrackCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.AddTrackSegmentCommand"/>
        public CompositeCommand AddTrackSegmentCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.AddTrackSegmentCommandParameters"/>
        public AddTrackSegmentCommandParameters AddTrackSegmentCommandParameters { get; }

        /// <inheritdoc cref="ITimelineCommands.RemoveTrackSegmentCommand"/>
        public CompositeCommand RemoveTrackSegmentCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.SplitSelectedTrackSegmentCommand"/>
        public CompositeCommand SplitSelectedTrackSegmentCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.SeekPreviousKeyFrameInTrackCommand"/>
        public CompositeCommand SeekPreviousKeyFrameInTrackCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.SeekNextKeyFrameInTrackCommand"/>
        public CompositeCommand SeekNextKeyFrameInTrackCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.AddTrackSegmentKeyFrameCommand"/>
        public CompositeCommand AddTrackSegmentKeyFrameCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.RemoveTrackSegmentKeyFrameCommand"/>
        public CompositeCommand RemoveTrackSegmentKeyFrameCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.CopyKeyFrameFromPreviousInTrackSegmentCommand"/>
        public CompositeCommand CopyKeyFrameFromPreviousInTrackSegmentCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.CopyKeyFrameFromNextInTrackSegmentCommand"/>
        public CompositeCommand CopyKeyFrameFromNextInTrackSegmentCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.CopyTrackSegmentKeyFrameToClipboardCommand"/>
        public CompositeCommand CopyTrackSegmentKeyFrameToClipboardCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.PasteTrackSegmentKeyFrameCommand"/>
        public CompositeCommand PasteTrackSegmentKeyFrameCommand { get; }

        /// <summary>
        /// Creates a new <see cref="TimelineCommands"/> instance.
        /// </summary>
        public TimelineCommands()
        {
            SetTimelineZoomLevelCommand = new CompositeCommand();
            AddTrackCommand = new CompositeCommand();
            RemoveTrackCommand = new CompositeCommand();
            AddTrackSegmentCommand = new CompositeCommand();
            AddTrackSegmentCommandParameters = new AddTrackSegmentCommandParameters();
            RemoveTrackSegmentCommand = new CompositeCommand();
            SplitSelectedTrackSegmentCommand = new CompositeCommand();
            SeekPreviousKeyFrameInTrackCommand = new CompositeCommand();
            SeekNextKeyFrameInTrackCommand = new CompositeCommand();
            AddTrackSegmentKeyFrameCommand = new CompositeCommand();
            RemoveTrackSegmentKeyFrameCommand = new CompositeCommand();
            CopyKeyFrameFromPreviousInTrackSegmentCommand = new CompositeCommand();
            CopyKeyFrameFromNextInTrackSegmentCommand = new CompositeCommand();
            CopyTrackSegmentKeyFrameToClipboardCommand = new CompositeCommand();
            PasteTrackSegmentKeyFrameCommand = new CompositeCommand();
        }
    }
}
