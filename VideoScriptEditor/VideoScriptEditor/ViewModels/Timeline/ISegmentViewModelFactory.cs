using System;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Models;

namespace VideoScriptEditor.ViewModels.Timeline
{
    /// <summary>
    /// Abstraction of a factory for creating view models derived from <see cref="SegmentViewModelBase"/>.
    /// </summary>
    public interface ISegmentViewModelFactory
    {
        /// <summary>
        /// Creates a new segment view model instance for coordinating interaction between a view and a given <see cref="SegmentModelBase">data model</see>.
        /// </summary>
        /// <param name="segmentModel">The <see cref="SegmentModelBase">data model</see> providing data for consumption by a view.</param>
        /// <returns>A new segment view model instance for coordinating interaction between a view and the specified <see cref="SegmentModelBase">data model</see>.</returns>
        SegmentViewModelBase CreateSegmentViewModel(SegmentModelBase segmentModel);

        /// <summary>
        /// Creates a new segment view model instance containing a new <see cref="SegmentModelBase">data model</see> instance
        /// of a type described by an enumeration value, with a specific track number, start frame number, end frame number
        /// and descriptive name.
        /// </summary>
        /// <param name="segmentTypeDescriptor">
        /// An <see cref="Enum"/> value describing the type of <see cref="SegmentModelBase">data model</see> to create.
        /// </param>
        /// <param name="trackNumber">The zero-based timeline track number of the segment.</param>
        /// <param name="startFrame">The inclusive zero-based start frame number of the segment.</param>
        /// <param name="endFrame">The inclusive zero-based end frame number of the segment.</param>
        /// <param name="name">A descriptive name for the segment. Defaults to a null <see cref="string"/>.</param>
        /// <returns>
        /// A new segment view model instance containing a new <see cref="SegmentModelBase">data model</see> instance
        /// of the described type, with the specified track number, start frame number, end frame number and descriptive name.
        /// </returns>
        SegmentViewModelBase CreateSegmentModelViewModel(Enum segmentTypeDescriptor, int trackNumber, int startFrame, int endFrame, string name = null);

        /// <summary>
        /// Creates a new segment view model instance containing a new <see cref="SegmentModelBase">data model</see> instance
        /// from data copied from another segment view model instance, optionally with a new track number, start frame number, end frame number,
        /// descriptive name and collection of key frame view models.
        /// </summary>
        /// <remarks>
        /// If a new start frame number is specified, the frame numbers of key frames deep copied from the <paramref name="sourceViewModel"/>
        /// instance are offset. If a new collection of key frame view models is specified, the frame numbers in the collection are
        /// assumed to be correct for the frame range of the new segment view model instance.
        /// </remarks>
        /// <param name="sourceViewModel">The source <see cref="SegmentViewModelBase"/> instance to copy data from.</param>
        /// <param name="trackNumber">
        /// The new zero-based timeline track number value. Copied from the <paramref name="sourceViewModel"/> if not specified.
        /// </param>
        /// <param name="startFrame">
        /// The new inclusive zero-based start frame number value. Copied from the <paramref name="sourceViewModel"/> if not specified.
        /// </param>
        /// <param name="endFrame">
        /// The new inclusive zero-based end frame number value. Copied from the <paramref name="sourceViewModel"/> if not specified.
        /// </param>
        /// <param name="name">
        /// The new descriptive name for the segment. Copied from the <paramref name="sourceViewModel"/> if not specified.
        /// </param>
        /// <param name="keyFrameViewModels">
        /// The sorted collection of key frame view models for the new segment view model instance.
        /// If not specified, created by deep copying the key frames from the <paramref name="sourceViewModel"/>.
        /// </param>
        /// <returns>
        /// A new segment view model instance containing a new <see cref="SegmentModelBase">data model</see> instance
        /// with data copied from the specified segment view model instance and/or the specified data values and key frame collection.
        /// </returns>
        SegmentViewModelBase CreateSegmentModelViewModel(SegmentViewModelBase sourceViewModel, int? trackNumber = null, int? startFrame = null, int? endFrame = null, string name = null, KeyFrameViewModelCollection keyFrameViewModels = null);

        /// <summary>
        /// Creates a new segment view model instance containing a new <see cref="SegmentModelBase">data model</see> instance
        /// by splitting an existing view model instance at a given frame number.
        /// </summary>
        /// <remarks>
        /// The existing view model's end frame number will be changed to the frame number before that specified by <paramref name="frameNumberToSplitAt"/>
        /// and key frames after that frame will be moved to the newly created segment view model.
        /// </remarks>
        /// <param name="segmentViewModelToSplit">The existing segment view model instance to split.</param>
        /// <param name="frameNumberToSplitAt">The zero-based frame number to split the existing view model at.</param>
        /// <returns>
        /// A new segment view model instance containing a new <see cref="SegmentModelBase">data model</see> instance
        /// that starts at the frame number specified by <paramref name="frameNumberToSplitAt"/>,
        /// ends at <paramref name="segmentViewModelToSplit"/>'s original end frame number and contains key frames moved from
        /// <paramref name="segmentViewModelToSplit"/> that start at or after the frame number specified by <paramref name="frameNumberToSplitAt"/>.
        /// </returns>
        SegmentViewModelBase CreateSplitSegmentModelViewModel(SegmentViewModelBase segmentViewModelToSplit, int frameNumberToSplitAt);
    }
}
