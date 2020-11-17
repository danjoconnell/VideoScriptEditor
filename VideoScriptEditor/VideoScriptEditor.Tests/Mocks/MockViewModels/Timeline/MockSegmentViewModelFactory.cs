using MonitoredUndo;
using System;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Models;
using VideoScriptEditor.Tests.Mocks.MockModels;
using VideoScriptEditor.ViewModels.Timeline;

namespace VideoScriptEditor.Tests.Mocks.MockViewModels.Timeline
{
    public class MockSegmentViewModelFactory : SegmentViewModelFactoryBase
    {
        public MockSegmentViewModelFactory(Services.ScriptVideo.IScriptVideoContext scriptVideoContext, IUndoService undoService, IChangeFactory undoChangeFactory, object rootUndoObject, Services.IClipboardService clipboardService) : base(scriptVideoContext, undoService, undoChangeFactory, rootUndoObject, clipboardService)
        {
        }

        /// <inheritdoc/>
        public override SegmentViewModelBase CreateSegmentViewModel(SegmentModelBase segmentModel)
        {
            return new MockSegmentViewModel((MockSegmentModel)segmentModel, _scriptVideoContext, _rootUndoObject, _undoService, _undoChangeFactory, _clipboardService);
        }

        /// <inheritdoc/>
        public override SegmentViewModelBase CreateSegmentModelViewModel(Enum segmentTypeDescriptor, int trackNumber, int startFrame, int endFrame, string name = null)
        {
            MockSegmentModel model = new MockSegmentModel(startFrame, endFrame, trackNumber,
                                                          new KeyFrameModelCollection()
                                                          {
                                                              new MockKeyFrameModel(startFrame)
                                                          },
                                                          name ?? segmentTypeDescriptor?.ToString());

            return new MockSegmentViewModel(model, _scriptVideoContext, _rootUndoObject, _undoService, _undoChangeFactory, _clipboardService);
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
                                                     : GetKeyFrameModels(sourceViewModel.KeyFrameViewModels, true, newStartFrame - sourceViewModel.StartFrame);

            return new MockSegmentViewModel(
                        new MockSegmentModel(newStartFrame, newEndFrame, newTrackNumber, keyFrameModels, newName),
                        _scriptVideoContext, _rootUndoObject, _undoService, _undoChangeFactory, _clipboardService, keyFrameViewModels
                   );
        }
    }
}
