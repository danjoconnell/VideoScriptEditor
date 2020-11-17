using MonitoredUndo;
using System;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Cropping;
using VideoScriptEditor.ViewModels.Timeline;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.ViewModels.Cropping
{
    /// <summary>
    /// A factory for creating <see cref="CropSegmentViewModel"/> instances.
    /// </summary>
    /// <inheritdoc cref="SegmentViewModelFactoryBase"/>
    public class CropSegmentViewModelFactory : SegmentViewModelFactoryBase
    {
        /// <summary>
        /// Creates a new <see cref="CropSegmentViewModelFactory"/> instance.
        /// </summary>
        /// <inheritdoc cref="SegmentViewModelFactoryBase(Services.ScriptVideo.IScriptVideoContext, IUndoService, IChangeFactory, object, Services.IClipboardService)"/>
        public CropSegmentViewModelFactory(Services.ScriptVideo.IScriptVideoContext scriptVideoContext, IUndoService undoService, IChangeFactory undoChangeFactory, object rootUndoObject, Services.IClipboardService clipboardService) : base(scriptVideoContext, undoService, undoChangeFactory, rootUndoObject, clipboardService)
        {
        }

        /// <inheritdoc/>
        public override SegmentViewModelBase CreateSegmentViewModel(SegmentModelBase segmentModel)
        {
            return new CropSegmentViewModel(segmentModel as CropSegmentModel, _scriptVideoContext, _rootUndoObject, _undoService, _undoChangeFactory, _clipboardService);
        }

        /// <inheritdoc/>
        public override SegmentViewModelBase CreateSegmentModelViewModel(Enum segmentTypeDescriptor, int trackNumber, int startFrame, int endFrame, string name = null)
        {
            if (segmentTypeDescriptor == null || !segmentTypeDescriptor.Equals(CropSegmentType.Crop))
            {
                throw new ArgumentException(nameof(segmentTypeDescriptor));
            }

            SizeI videoFrameSize = _scriptVideoContext.VideoFrameSize;

            CropSegmentModel model = new CropSegmentModel(startFrame, endFrame, trackNumber,
                                                          new KeyFrameModelCollection()
                                                          {
                                                              new CropKeyFrameModel(startFrame, 0d, 0d,
                                                                                    videoFrameSize.Width,
                                                                                    videoFrameSize.Height, 0d)
                                                          },
                                                          name ?? segmentTypeDescriptor?.ToString());

            return new CropSegmentViewModel(model, _scriptVideoContext, _rootUndoObject, _undoService, _undoChangeFactory, _clipboardService);
        }

        /// <inheritdoc/>
        public override SegmentViewModelBase CreateSegmentModelViewModel(SegmentViewModelBase sourceViewModel, int? trackNumber = null, int? startFrame = null, int? endFrame = null, string name = null, KeyFrameViewModelCollection keyFrameViewModels = null)
        {
            int newTrackNumber = trackNumber.GetValueOrDefault(sourceViewModel.TrackNumber);
            int newStartFrame = startFrame.GetValueOrDefault(sourceViewModel.StartFrame);
            int newEndFrame = endFrame.GetValueOrDefault(sourceViewModel.EndFrame);
            string newName = name ?? sourceViewModel.Name;

            KeyFrameModelCollection keyFrameModels = keyFrameViewModels != null
                                                     ? GetKeyFrameModels(keyFrameViewModels)
                                                     : GetKeyFrameModels(sourceViewModel.KeyFrameViewModels,
                                                                         createCopies: true,
                                                                         frameOffset: newStartFrame - sourceViewModel.StartFrame);

            return new CropSegmentViewModel(
                        new CropSegmentModel(newStartFrame, newEndFrame, newTrackNumber, keyFrameModels, newName),
                        _scriptVideoContext, _rootUndoObject, _undoService, _undoChangeFactory, _clipboardService, keyFrameViewModels
                    );
        }
    }
}
