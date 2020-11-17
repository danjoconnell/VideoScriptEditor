using Prism.Mvvm;

namespace VideoScriptEditor.ViewModels.Timeline
{
    /// <summary>
    /// View Model encapsulating presentation logic for Track Segment Adorner ToolTips.
    /// </summary>
    public class TrackSegmentAdornerToolTipViewModel : BindableBase
    {
        private int _startFrame;
        private int _endFrame;

        /// <summary>
        /// The inclusive zero-based start frame number of the track segment.
        /// </summary>
        public int StartFrame
        {
            get => _startFrame;
            set => SetProperty(ref _startFrame, value);
        }

        /// <summary>
        /// The inclusive zero-based end frame number of the track segment.
        /// </summary>
        public int EndFrame
        {
            get => _endFrame;
            set => SetProperty(ref _endFrame, value);
        }

        /// <summary>
        /// Creates a new <see cref="TrackSegmentAdornerToolTipViewModel"/> instance.
        /// </summary>
        public TrackSegmentAdornerToolTipViewModel()
        {
        }
    }
}
