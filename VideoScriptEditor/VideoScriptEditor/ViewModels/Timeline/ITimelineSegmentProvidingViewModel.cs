using MonitoredUndo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Models;
using VideoScriptEditor.Services.ScriptVideo;

namespace VideoScriptEditor.ViewModels.Timeline
{
    /// <summary>
    /// Interface abstracting a view model that provides segments to the <see cref="IVideoTimelineViewModel">timeline</see>.
    /// </summary>
    public interface ITimelineSegmentProvidingViewModel : INotifyPropertyChanged, ISupportsUndo
    {
        /// <summary>
        /// Occurs when the value of the <see cref="SelectedSegment"/> property changes.
        /// </summary>
        event EventHandler SelectedSegmentChanged;

        /// <summary>
        /// Gets a sorted collection of <see cref="SegmentModelBase">segment models</see>.
        /// </summary>
        SegmentModelCollection SegmentModels { get; }

        /// <summary>
        /// Gets a sorted collection of <see cref="SegmentViewModelBase">segment view models</see>.
        /// </summary>
        SegmentViewModelCollection SegmentViewModels { get; }

        /// <summary>
        /// Gets an instance of a factory for creating segment view models.
        /// </summary>
        ISegmentViewModelFactory SegmentViewModelFactory { get; }

        /// <summary>
        /// Gets a read-only sorted collection of 'Active' <see cref="SegmentViewModelBase">segments</see>.
        /// </summary>
        /// <remarks>
        /// Active segments are those whose frame range includes the current <see cref="IScriptVideoContext.FrameNumber">frame number</see>.
        /// </remarks>
        IList<SegmentViewModelBase> ActiveSegments { get; }

        /// <summary>
        /// Gets or sets the currently selected <see cref="SegmentViewModelBase">segment</see>.
        /// </summary>
        SegmentViewModelBase SelectedSegment { get; set; }

        /// <summary>
        /// Gets or sets the zero-based track number of the active (currently selected) track in the timeline.
        /// </summary>
        int ActiveTrackNumber { get; set; }

        /// <summary>
        /// Gets the runtime context of the <see cref="Services.ScriptVideo.IScriptVideoService"/> instance.
        /// </summary>
        IScriptVideoContext ScriptVideoContext { get; }

        /// <summary>
        /// Refreshes the <see cref="ActiveSegments"/> collection.
        /// </summary>
        /// <remarks>
        /// Filters the <see cref="SegmentViewModels"/> collection by those whose frame range
        /// includes the current <see cref="IScriptVideoContext.FrameNumber">frame number</see>.
        /// </remarks>
        void RefreshActiveSegments();
    }
}
