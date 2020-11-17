using Prism.Mvvm;
using System;

namespace VideoScriptEditor.Commands
{
    /// <summary>
    /// Encapsulates parameters for the <see cref="ITimelineCommands.AddTrackSegmentCommand"/>.
    /// </summary>
    public class AddTrackSegmentCommandParameters : BindableBase
    {
        private Enum _segmentTypeDescriptor;
        private int _frameDuration;

        /// <summary>
        /// An <see cref="Enum"/> value describing the type of track segment to create and add to the timeline.
        /// </summary>
        public Enum SegmentTypeDescriptor
        {
            get => _segmentTypeDescriptor;
            set => SetProperty(ref _segmentTypeDescriptor, value);
        }

        /// <summary>
        /// The inclusive frame duration of the new track segment.
        /// </summary>
        public int FrameDuration
        {
            get => _frameDuration;
            set => SetProperty(ref _frameDuration, value);
        }

        /// <summary>
        /// Creates a new <see cref="AddTrackSegmentCommandParameters"/> instance.
        /// </summary>
        public AddTrackSegmentCommandParameters()
        {
        }
    }
}
