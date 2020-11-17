using MonitoredUndo;
using System;
using System.Diagnostics.CodeAnalysis;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Models;
using VideoScriptEditor.Tests.Mocks.MockModels;
using VideoScriptEditor.ViewModels.Timeline;

namespace VideoScriptEditor.Tests.Mocks.MockViewModels.Timeline
{
    public class MockSegmentViewModel : SegmentViewModelBase, IEquatable<MockSegmentViewModel>
    {
        private MockKeyFrameViewModel _activeKeyFrame = null;
        public override KeyFrameViewModelBase ActiveKeyFrame
        {
            get => _activeKeyFrame;
            set
            {
                if (_activeKeyFrame != null)
                {
                    _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;
                    _activeKeyFrame.IsActive = false;
                }

                SetProperty(ref _activeKeyFrame, (MockKeyFrameViewModel)value, OnActiveKeyFrameChanged);
            }
        }

        public MockSegmentViewModel(SegmentModelBase model, Services.ScriptVideo.IScriptVideoContext scriptVideoContext, object rootUndoObject, IUndoService undoService, IChangeFactory undoChangeFactory, Services.IClipboardService clipboardService, KeyFrameViewModelCollection keyFrameViewModels = null) : base(model, scriptVideoContext, rootUndoObject, undoService, undoChangeFactory, clipboardService, keyFrameViewModels)
        {
        }

        public override void AddKeyFrame()
        {
            System.Diagnostics.Debug.Assert(ActiveKeyFrame == null);

            // Batch undo ChangeSet for Model & ViewModel adds so that any subsequent property change undo/redo won't be performed on orphaned ViewModels (Undo references an existing ViewModel instance in property ChangeSets)
            _undoRoot.BeginChangeSetBatch("Mock key frame added", false);

            MockKeyFrameModel keyFrameModel = new MockKeyFrameModel(ScriptVideoContext.FrameNumber);
            MockKeyFrameViewModel keyFrameViewModel = new MockKeyFrameViewModel(keyFrameModel, _rootUndoObject, _undoService, _undoChangeFactory);

            Model.KeyFrames.Add(keyFrameModel);
            KeyFrameViewModels.Add(keyFrameViewModel);

            _undoRoot.EndChangeSetBatch();
        }

        protected override void CopyFromKeyFrameModel(KeyFrameModelBase keyFrameModel)
        {
        }

        public override void Lerp(KeyFrameViewModelBase fromKeyFrame, KeyFrameViewModelBase toKeyFrame, double amount)
        {
            // MockKeyFrameViewModel only has FrameNumber property, so nothing testable to Lerp
        }

        protected override KeyFrameViewModelBase CreateKeyFrameViewModel(KeyFrameModelBase keyFrameModel)
        {
            return new MockKeyFrameViewModel(keyFrameModel, _rootUndoObject, _undoService, _undoChangeFactory);
        }

        protected override bool CanCopyKeyFrameViewModelToClipboard(KeyFrameViewModelBase keyFrameViewModel)
        {
            return keyFrameViewModel is MockKeyFrameViewModel || ActiveKeyFrame != null;
        }

        protected override void CopyKeyFrameModelToClipboard(KeyFrameModelBase keyFrameModel)
        {
            _clipboardService.SetData((MockKeyFrameModel)keyFrameModel);
        }

        protected override bool CanPasteKeyFrameFromClipboard(KeyFrameViewModelBase targetKeyFrame)
        {
            return _clipboardService.ContainsData<MockKeyFrameModel>();
        }

        protected override void PasteKeyFrameFromClipboard(KeyFrameViewModelBase targetKeyFrame)
        {
            PasteKeyFrameModel(_clipboardService.GetData<MockKeyFrameModel>(),
                               targetKeyFrame);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as MockSegmentViewModel);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] MockSegmentViewModel other)
        {
            // If parameter is null, return false.
            if (other is null)
            {
                return false;
            }

            // Optimization for a common success case.
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // Let base class check its own fields
            // and do the run-time type comparison.
            return base.Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode());
        }
    }
}