using MonitoredUndo;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using VideoScriptEditor.Extensions;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Masking.Shapes;
using VideoScriptEditor.ViewModels.Timeline;

namespace VideoScriptEditor.ViewModels.Masking.Shapes
{
    /// <summary>
    /// View Model for coordinating interaction between a view and a <see cref="RectangleMaskShapeKeyFrameModel">rectangle masking key frame model</see>.
    /// </summary>
    public class RectangleMaskShapeKeyFrameViewModel : KeyFrameViewModelBase, IEquatable<RectangleMaskShapeKeyFrameViewModel>
    {
        private RectangleMaskShapeKeyFrameModel _model;

        /// <inheritdoc cref="KeyFrameViewModelBase.Model"/>
        public override KeyFrameModelBase Model
        {
            get => _model;
            protected set => _model = (RectangleMaskShapeKeyFrameModel)value;
        }

        /// <inheritdoc cref="RectangleMaskShapeKeyFrameModel.Left"/>
        public double Left
        {
            get => _model.Left;
            set
            {
                if (_model.Left != value)
                {
                    _undoChangeFactory.OnChanging(this, nameof(Left), _model.Left, value);

                    _model.Left = value;
                    RaisePropertyChanged();
                }
            }
        }


        /// <inheritdoc cref="RectangleMaskShapeKeyFrameModel.Top"/>
        public double Top
        {
            get => _model.Top;
            set
            {
                if (_model.Top != value)
                {
                    _undoChangeFactory.OnChanging(this, nameof(Top), _model.Top, value);

                    _model.Top = value;
                    RaisePropertyChanged();
                }
            }
        }


        /// <inheritdoc cref="RectangleMaskShapeKeyFrameModel.Width"/>
        public double Width
        {
            get => _model.Width;
            set
            {
                if (_model.Width != value)
                {
                    _undoChangeFactory.OnChanging(this, nameof(Width), _model.Width, value);

                    _model.Width = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <inheritdoc cref="RectangleMaskShapeKeyFrameModel.Height"/>
        public double Height
        {
            get => _model.Height;
            set
            {
                if (_model.Height != value)
                {
                    _undoChangeFactory.OnChanging(this, nameof(Height), _model.Height, value);

                    _model.Height = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// The bounds of the rectangle.
        /// </summary>
        public Rect Bounds => _model.ToRect();

        /// <summary>
        /// Creates a new <see cref="RectangleMaskShapeKeyFrameViewModel"/> instance.
        /// </summary>
        /// <param name="model">The <see cref="RectangleMaskShapeKeyFrameModel">rectangle masking key frame model</see> providing data for consumption by a view.</param>
        /// <inheritdoc cref="KeyFrameViewModelBase(KeyFrameModelBase, object, IUndoService, IChangeFactory)"/>
        public RectangleMaskShapeKeyFrameViewModel(RectangleMaskShapeKeyFrameModel model, object rootUndoObject, IUndoService undoService, IChangeFactory undoChangeFactory) : base(model, rootUndoObject, undoService, undoChangeFactory)
        {
        }

        /// <inheritdoc/>
        public override KeyFrameViewModelBase Lerp(int fromFrameNumber, KeyFrameViewModelBase toKeyFrameViewModel)
        {
            if (toKeyFrameViewModel is not RectangleMaskShapeKeyFrameViewModel toRectangleKeyFrameViewModel)
            {
                throw new ArgumentException(nameof(toKeyFrameViewModel));
            }

            int frameRange = toKeyFrameViewModel.FrameNumber - FrameNumber;
            double lerpAmount = (frameRange > 0) ? (double)(fromFrameNumber - FrameNumber) / frameRange : 0d; // prevent double.NaN value as a result of division by zero

            return new RectangleMaskShapeKeyFrameViewModel(
                new RectangleMaskShapeKeyFrameModel(fromFrameNumber,
                                                    Left.LerpTo(toRectangleKeyFrameViewModel.Left, lerpAmount),
                                                    Top.LerpTo(toRectangleKeyFrameViewModel.Top, lerpAmount),
                                                    Width.LerpTo(toRectangleKeyFrameViewModel.Width, lerpAmount),
                                                    Height.LerpTo(toRectangleKeyFrameViewModel.Height, lerpAmount)),
                _rootUndoObject, _undoService, _undoChangeFactory
            );
        }

        /// <inheritdoc/>
        public override void CopyFromModel(KeyFrameModelBase keyFrameModel)
        {
            if (keyFrameModel is not RectangleMaskShapeKeyFrameModel rectangleKeyFrameModel)
            {
                throw new ArgumentException(nameof(keyFrameModel));
            }

            _undoRoot.BeginChangeSetBatch("Rectangle mask shape key frame copied", false);

            Left = rectangleKeyFrameModel.Left;
            Top = rectangleKeyFrameModel.Top;
            Width = rectangleKeyFrameModel.Width;
            Height = rectangleKeyFrameModel.Height;

            _undoRoot.EndChangeSetBatch();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as RectangleMaskShapeKeyFrameViewModel);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] RectangleMaskShapeKeyFrameViewModel other)
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
