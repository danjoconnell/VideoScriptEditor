using VideoScriptEditor.ViewModels.Timeline;

namespace VideoScriptEditor.ViewModels.Cropping
{
    /// <summary>
    /// Interface abstracting a view model that encapsulates presentation logic for cropping related views.
    /// </summary>
    public interface ICroppingViewModel : ITimelineSegmentProvidingViewModel
    {
        /// <summary>
        /// Gets or sets a value specifying which adjustment handles should be displayed around
        /// the <see cref="ITimelineSegmentProvidingViewModel.SelectedSegment">selected crop segment</see>
        /// in the view; <see cref="CropAdjustmentHandleMode.Resize">resize</see>
        /// or <see cref="CropAdjustmentHandleMode.Rotate">rotate</see>.
        /// </summary>
        CropAdjustmentHandleMode AdjustmentHandleMode { get; set; }
    }
}
