using MonitoredUndo;
using System;
using System.Diagnostics.CodeAnalysis;
using VideoScriptEditor.Models;
using VideoScriptEditor.Tests.Mocks.MockModels;
using VideoScriptEditor.ViewModels.Timeline;

namespace VideoScriptEditor.Tests.Mocks.MockViewModels.Timeline
{
    public class MockKeyFrameViewModel : KeyFrameViewModelBase, IEquatable<MockKeyFrameViewModel>
    {
        private MockKeyFrameModel _model;
        public override KeyFrameModelBase Model
        {
            get => _model;
            protected set => _model = (MockKeyFrameModel)value;
        }

        public MockKeyFrameViewModel(KeyFrameModelBase model, object rootUndoObject, IUndoService undoService, IChangeFactory undoChangeFactory) : base(model, rootUndoObject, undoService, undoChangeFactory)
        {
            if (model is not MockKeyFrameModel)
            {
                throw new ArgumentException(nameof(model));
            }
        }

        public override KeyFrameViewModelBase Lerp(int fromFrameNumber, KeyFrameViewModelBase toKeyFrameViewModel)
        {
            if (toKeyFrameViewModel is not MockKeyFrameViewModel)
            {
                throw new ArgumentException(nameof(toKeyFrameViewModel));
            }

            KeyFrameModelBase lerpedModel = _model.DeepCopy();
            lerpedModel.FrameNumber = fromFrameNumber;

            return new MockKeyFrameViewModel(lerpedModel, _rootUndoObject, _undoService, _undoChangeFactory);
        }

        public override void CopyFromModel(KeyFrameModelBase keyFrameModel)
        {
            if (keyFrameModel is not MockKeyFrameModel)
            {
                throw new ArgumentException(nameof(keyFrameModel));
            }

            FrameNumber = keyFrameModel.FrameNumber;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as MockKeyFrameViewModel);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] MockKeyFrameViewModel other)
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

            // Check properties that this class declares
            // and let base class check its own fields and do the run-time type comparison.
            return _model.Equals(other._model) && base.Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode());
        }
    }
}
