using MonitoredUndo;
using System;
using System.Linq;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Models;
using VideoScriptEditor.Services;
using VideoScriptEditor.Services.ScriptVideo;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.ViewModels.Timeline
{
    /// <summary>
    /// Base class for factories that create view models derived from <see cref="SegmentViewModelBase"/>.
    /// </summary>
    /// <remarks>Implements <see cref="ISegmentViewModelFactory"/>.</remarks>
    public abstract class SegmentViewModelFactoryBase : ISegmentViewModelFactory
    {
        /// <summary>The runtime context of the <see cref="IScriptVideoService"/> instance.</summary>
        protected readonly IScriptVideoContext _scriptVideoContext;

        /// <summary>The <see cref="IUndoService"/> instance providing undo/redo support.</summary>
        protected readonly IUndoService _undoService;

        /// <summary>The <see cref="IChangeFactory"/> instance for undo <see cref="Change"/> creation.</summary>
        protected readonly IChangeFactory _undoChangeFactory;

        /// <summary>The undo "root document" or "root object" for the view models this factory creates.</summary>
        protected readonly object _rootUndoObject;

        /// <summary>The <see cref="IClipboardService"/> instance providing access to the system clipboard.</summary>
        protected readonly IClipboardService _clipboardService;

        /// <summary>
        /// Base constructor for segment view model factories derived from the <see cref="SegmentViewModelFactoryBase"/> class. 
        /// </summary>
        /// <param name="scriptVideoContext">The runtime context of the <see cref="IScriptVideoService"/> instance.</param>
        /// <param name="undoService">The <see cref="IUndoService"/> instance providing undo/redo support.</param>
        /// <param name="undoChangeFactory">The <see cref="IChangeFactory"/> instance for undo <see cref="Change"/> creation.</param>
        /// <param name="rootUndoObject">The undo "root document" or "root object" for the view models this factory creates.</param>
        /// <param name="clipboardService">The <see cref="IClipboardService"/> instance providing access to the system clipboard.</param>
        protected SegmentViewModelFactoryBase(IScriptVideoContext scriptVideoContext, IUndoService undoService, IChangeFactory undoChangeFactory, object rootUndoObject, IClipboardService clipboardService)
        {
            _scriptVideoContext = scriptVideoContext;
            _undoService = undoService;
            _undoChangeFactory = undoChangeFactory;
            _rootUndoObject = rootUndoObject;
            _clipboardService = clipboardService;
        }

        /// <inheritdoc/>
        public abstract SegmentViewModelBase CreateSegmentViewModel(SegmentModelBase segmentModel);

        /// <inheritdoc/>
        public abstract SegmentViewModelBase CreateSegmentModelViewModel(Enum segmentTypeDescriptor, int trackNumber, int startFrame, int endFrame, string name = null);

        /// <inheritdoc/>
        public abstract SegmentViewModelBase CreateSegmentModelViewModel(SegmentViewModelBase sourceViewModel, int? trackNumber = null, int? startFrame = null, int? endFrame = null, string name = null, KeyFrameViewModelCollection keyFrameViewModels = null);

        /// <inheritdoc/>
        public virtual SegmentViewModelBase CreateSplitSegmentModelViewModel(SegmentViewModelBase segmentViewModelToSplit, int frameNumberToSplitAt)
        {
            KeyFrameViewModelCollection splitKeyFrameViewModels = new KeyFrameViewModelCollection();

            if (segmentViewModelToSplit.ActiveKeyFrame?.FrameNumber >= frameNumberToSplitAt)
            {
                segmentViewModelToSplit.ActiveKeyFrame = null;
            }

            int keyFrameIndex = segmentViewModelToSplit.KeyFrameViewModels.LowerBoundIndex(frameNumberToSplitAt);
            if (keyFrameIndex < segmentViewModelToSplit.KeyFrameViewModels.Count)
            {
                KeyFrameViewModelBase keyFrameViewModel = segmentViewModelToSplit.KeyFrameViewModels[keyFrameIndex];
                if (keyFrameViewModel.FrameNumber > frameNumberToSplitAt)
                {
                    // Lerp new KeyFrameViewModel and add to splitKeyFrameViewModels
                    Debug.Assert(keyFrameIndex > 0);

                    keyFrameViewModel = segmentViewModelToSplit.KeyFrameViewModels[keyFrameIndex - 1].Lerp(frameNumberToSplitAt, keyFrameViewModel);

                    splitKeyFrameViewModels.Add(keyFrameViewModel);
                }

                // Loop from keyFrameIndex and move (via remove & add) KeyFrameViewModel(s) to splitKeyFrameViewModels
                do
                {
                    keyFrameViewModel = segmentViewModelToSplit.KeyFrameViewModels[keyFrameIndex];

                    segmentViewModelToSplit.KeyFrameViewModels.RemoveAt(keyFrameIndex);
                    segmentViewModelToSplit.Model.KeyFrames.RemoveAt(keyFrameIndex);

                    splitKeyFrameViewModels.Add(keyFrameViewModel);

                } while (keyFrameIndex < segmentViewModelToSplit.KeyFrameViewModels.Count);
            }
            else
            {
                // Lerp new KeyFrameViewModel and add to splitKeyFrameViewModels
                KeyFrameModelBase keyFrameModel = segmentViewModelToSplit.Model.KeyFrames.Last(kfm => kfm.FrameNumber < frameNumberToSplitAt).DeepCopy();
                keyFrameModel.FrameNumber = frameNumberToSplitAt;

                splitKeyFrameViewModels.Add(segmentViewModelToSplit.CreateKeyFrameViewModel(keyFrameModel));
            }

            int splitViewModelEndFrame = segmentViewModelToSplit.EndFrame;
            segmentViewModelToSplit.EndFrame = frameNumberToSplitAt - 1;

            return CreateSegmentModelViewModel(segmentViewModelToSplit, segmentViewModelToSplit.TrackNumber,
                                               frameNumberToSplitAt, splitViewModelEndFrame, segmentViewModelToSplit.Name,
                                               splitKeyFrameViewModels);
        }

        /// <summary>
        /// Gets a <see cref="KeyFrameModelCollection"/> containing the data models of key frame view models in a <see cref="KeyFrameViewModelCollection"/>,
        /// optionally creating deep copies of the data models and offsetting the key frame numbers.
        /// </summary>
        /// <param name="keyFrameViewModels">The source <see cref="KeyFrameViewModelCollection"/>.</param>
        /// <param name="createCopies">Whether to create deep copies of the data models. Defaults to false.</param>
        /// <param name="frameOffset">The number of frames to offset the frame numbers of the data models by. Defaults to zero.</param>
        /// <returns>
        /// A <see cref="KeyFrameModelCollection"/> containing the data models of key frame view models in the <see cref="KeyFrameViewModelCollection"/>
        /// which have been deep copied and/or frame offset if so specified.
        /// </returns>
        protected KeyFrameModelCollection GetKeyFrameModels(KeyFrameViewModelCollection keyFrameViewModels, bool createCopies = false, int frameOffset = 0)
        {
            KeyFrameModelCollection keyFrameModels = new KeyFrameModelCollection();

            for (int i = 0; i < keyFrameViewModels.Count; i++)
            {
                KeyFrameModelBase keyFrameModel = createCopies ? keyFrameViewModels[i].Model.DeepCopy() : keyFrameViewModels[i].Model;
                if (frameOffset != 0)
                {
                    keyFrameModel.FrameNumber += frameOffset;
                }

                keyFrameModels.Add(keyFrameModel);
            }

            return keyFrameModels;
        }
    }
}
