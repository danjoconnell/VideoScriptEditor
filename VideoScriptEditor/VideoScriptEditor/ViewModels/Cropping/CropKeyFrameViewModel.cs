using MonitoredUndo;
using System;
using System.Windows;
using VideoScriptEditor.Geometry;
using VideoScriptEditor.Extensions;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Cropping;
using VideoScriptEditor.ViewModels.Timeline;
using System.Diagnostics.CodeAnalysis;

namespace VideoScriptEditor.ViewModels.Cropping
{
    /// <summary>
    /// View Model for coordinating interaction between a view and a <see cref="CropKeyFrameModel">crop key frame model</see>.
    /// </summary>
    public class CropKeyFrameViewModel : KeyFrameViewModelBase, IEquatable<CropKeyFrameViewModel>
    {
        private CropKeyFrameModel _model;

        /// <inheritdoc cref="KeyFrameViewModelBase.Model"/>
        public override KeyFrameModelBase Model
        {
            get => _model;
            protected set => _model = (CropKeyFrameModel)value;
        }

        /// <inheritdoc cref="CropKeyFrameModel.Left"/>
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
                    RaisePropertyChanged(nameof(Center));
                }
            }
        }

        /// <inheritdoc cref="CropKeyFrameModel.Top"/>
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
                    RaisePropertyChanged(nameof(Center));
                }
            }
        }

        /// <inheritdoc cref="CropKeyFrameModel.Width"/>
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
                    RaisePropertyChanged(nameof(Center));
                }
            }
        }

        /// <inheritdoc cref="CropKeyFrameModel.Height"/>
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
                    RaisePropertyChanged(nameof(Center));
                }
            }
        }

        /// <inheritdoc cref="CropKeyFrameModel.Angle"/>
        public double Angle
        {
            get => _model.Angle;
            set
            {
                if (_model.Angle != value)
                {
                    _undoChangeFactory.OnChanging(this, nameof(Angle), _model.Angle, value);

                    _model.Angle = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// The center point of the area to crop.
        /// </summary>
        public Point Center
        {
            get => PointExt.MidPoint(new Point(Left, Top), new Point(Left + Width, Top + Height));
            set
            {
                if (Center != value)
                {
                    Vector centerOffset = value - Center;

                    _undoRoot.BeginChangeSetBatch("Crop Center changed", true);

                    Left += centerOffset.X;
                    Top += centerOffset.Y;

                    _undoRoot.EndChangeSetBatch();
                }
            }
        }

        /// <summary>
        /// A <see cref="System.Windows.Rect"/> structure encapsulating the
        /// <see cref="Left">Left</see>, <see cref="Top">Top</see>,
        /// <see cref="Width">Width</see> and <see cref="Height">Height</see>
        /// property values.
        /// </summary>
        public Rect Rect
        {
            get => _model.BaseRect();
            set
            {
                if (Left != value.Left
                    || Top != value.Top
                    || Width != value.Width
                    || Height != value.Height)
                {
                    // Better performance if changes are included in an already running undo batch rather than a new sub-batch
                    bool batchUndoChanges = !_undoRoot.IsInBatch;
                    if (batchUndoChanges)
                        _undoRoot.BeginChangeSetBatch("Crop key frame Rect changed", true);

                    Left = value.Left;
                    Top = value.Top;
                    Width = value.Width;
                    Height = value.Height;

                    if (batchUndoChanges)
                        _undoRoot.EndChangeSetBatch();

                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="CropKeyFrameViewModel"/> instance.
        /// </summary>
        /// <param name="model">The <see cref="CropKeyFrameModel">crop key frame model</see> providing data for consumption by a view.</param>
        /// <inheritdoc cref="KeyFrameViewModelBase(KeyFrameModelBase, object, IUndoService, IChangeFactory)"/>
        public CropKeyFrameViewModel(CropKeyFrameModel model, object rootUndoObject, IUndoService undoService, IChangeFactory undoChangeFactory) : base(model, rootUndoObject, undoService, undoChangeFactory)
        {
        }

        /// <inheritdoc/>
        public override KeyFrameViewModelBase Lerp(int fromFrameNumber, KeyFrameViewModelBase toKeyFrameViewModel)
        {
            if (toKeyFrameViewModel is not CropKeyFrameViewModel toCropKeyFrameViewModel)
            {
                throw new ArgumentException(nameof(toKeyFrameViewModel));
            }

            int frameRange = toKeyFrameViewModel.FrameNumber - FrameNumber;
            double lerpAmount = (frameRange > 0) ? (double)(fromFrameNumber - FrameNumber) / frameRange : 0d; // prevent double.NaN value as a result of division by zero

            return new CropKeyFrameViewModel(new CropKeyFrameModel(fromFrameNumber,
                                                                   Left.LerpTo(toCropKeyFrameViewModel.Left, lerpAmount),
                                                                   Top.LerpTo(toCropKeyFrameViewModel.Top, lerpAmount),
                                                                   Width.LerpTo(toCropKeyFrameViewModel.Width, lerpAmount),
                                                                   Height.LerpTo(toCropKeyFrameViewModel.Height, lerpAmount),
                                                                   Angle == toCropKeyFrameViewModel.Angle ? Angle : Angle.LerpTo(toCropKeyFrameViewModel.Angle, lerpAmount)),
                                             _rootUndoObject, _undoService, _undoChangeFactory);
        }

        /// <inheritdoc/>
        public override void CopyFromModel(KeyFrameModelBase keyFrameModel)
        {
            if (keyFrameModel is not CropKeyFrameModel cropKeyFrameModel)
            {
                throw new ArgumentException(nameof(keyFrameModel));
            }

            _undoRoot.BeginChangeSetBatch("Crop key frame copied", false);

            Left = cropKeyFrameModel.Left;
            Top = cropKeyFrameModel.Top;
            Width = cropKeyFrameModel.Width;
            Height = cropKeyFrameModel.Height;
            Angle = cropKeyFrameModel.Angle;

            _undoRoot.EndChangeSetBatch();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as CropKeyFrameViewModel);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] CropKeyFrameViewModel other)
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
