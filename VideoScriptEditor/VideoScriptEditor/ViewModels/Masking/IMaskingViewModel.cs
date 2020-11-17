using System;
using VideoScriptEditor.ViewModels.Masking.Shapes;

namespace VideoScriptEditor.ViewModels.Masking
{
    /// <summary>
    /// Interface abstracting a view model that encapsulates presentation logic for masking related views.
    /// </summary>
    public interface IMaskingViewModel : Timeline.ITimelineSegmentProvidingViewModel
    {
        /// <summary>
        /// Gets or sets a value specifying the method for resizing a masking shape.
        /// </summary>
        MaskShapeResizeMode? ShapeResizeMode { get; set; }

        /// <summary>
        /// Occurs when the value of the <see cref="ShapeResizeMode"/> property changes.
        /// </summary>
        event EventHandler ShapeResizeModeChanged;
    }
}
