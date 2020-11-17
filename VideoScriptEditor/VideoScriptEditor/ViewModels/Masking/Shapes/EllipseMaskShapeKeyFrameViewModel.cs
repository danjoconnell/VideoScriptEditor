using MonitoredUndo;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using VideoScriptEditor.Extensions;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Masking.Shapes;
using VideoScriptEditor.Models.Primitives;
using VideoScriptEditor.ViewModels.Timeline;

namespace VideoScriptEditor.ViewModels.Masking.Shapes
{
    /// <summary>
    /// View Model for coordinating interaction between a view and an <see cref="EllipseMaskShapeKeyFrameModel">ellipse masking key frame model</see>.
    /// </summary>
    public class EllipseMaskShapeKeyFrameViewModel : KeyFrameViewModelBase, IEquatable<EllipseMaskShapeKeyFrameViewModel>
    {
        private EllipseMaskShapeKeyFrameModel _model;

        /// <inheritdoc cref="KeyFrameViewModelBase.Model"/>
        public override KeyFrameModelBase Model
        {
            get => _model;
            protected set => _model = (EllipseMaskShapeKeyFrameModel)value;
        }

        /// <inheritdoc cref="EllipseMaskShapeKeyFrameModel.CenterPoint"/>
        public Point CenterPoint
        {
            get => _model.CenterPoint.ToWpfPoint();
            set
            {
                if (!_model.CenterPoint.IsEqualTo(value))
                {
                    PointD newValue = value.ToPointD();
                    _undoChangeFactory.OnChanging(this, nameof(CenterPoint), _model.CenterPoint, newValue);

                    _model.CenterPoint = newValue;
                    RaisePropertyChanged();
                }
            }
        }

        /// <inheritdoc cref="EllipseMaskShapeKeyFrameModel.RadiusX"/>
        public double RadiusX
        {
            get => _model.RadiusX;
            set
            {
                if (_model.RadiusX != value)
                {
                    _undoChangeFactory.OnChanging(this, nameof(RadiusX), _model.RadiusX, value);

                    _model.RadiusX = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <inheritdoc cref="EllipseMaskShapeKeyFrameModel.RadiusY"/>
        public double RadiusY
        {
            get => _model.RadiusY;
            set
            {
                if (_model.RadiusY != value)
                {
                    _undoChangeFactory.OnChanging(this, nameof(RadiusY), _model.RadiusY, value);

                    _model.RadiusY = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="EllipseMaskShapeKeyFrameViewModel"/> instance.
        /// </summary>
        /// <param name="model">The <see cref="EllipseMaskShapeKeyFrameModel">ellipse masking key frame model</see> providing data for consumption by a view.</param>
        /// <inheritdoc cref="KeyFrameViewModelBase(KeyFrameModelBase, object, IUndoService, IChangeFactory)"/>
        public EllipseMaskShapeKeyFrameViewModel(EllipseMaskShapeKeyFrameModel model, object rootUndoObject, IUndoService undoService, IChangeFactory undoChangeFactory) : base(model, rootUndoObject, undoService, undoChangeFactory)
        {
        }

        /// <inheritdoc/>
        public override KeyFrameViewModelBase Lerp(int fromFrameNumber, KeyFrameViewModelBase toKeyFrameViewModel)
        {
            if (toKeyFrameViewModel is not EllipseMaskShapeKeyFrameViewModel toEllipseKeyFrameViewModel)
            {
                throw new ArgumentException(nameof(toKeyFrameViewModel));
            }

            int frameRange = toKeyFrameViewModel.FrameNumber - FrameNumber;
            double lerpAmount = (frameRange > 0) ? (double)(fromFrameNumber - FrameNumber) / frameRange : 0d; // prevent double.NaN value as a result of division by zero

            return new EllipseMaskShapeKeyFrameViewModel(
                new EllipseMaskShapeKeyFrameModel(fromFrameNumber,
                                                  PointD.Lerp(_model.CenterPoint, toEllipseKeyFrameViewModel._model.CenterPoint, lerpAmount),
                                                  RadiusX.LerpTo(toEllipseKeyFrameViewModel.RadiusX, lerpAmount),
                                                  RadiusY.LerpTo(toEllipseKeyFrameViewModel.RadiusY, lerpAmount)),
                _rootUndoObject, _undoService, _undoChangeFactory
            );
        }

        /// <inheritdoc/>
        public override void CopyFromModel(KeyFrameModelBase keyFrameModel)
        {
            if (keyFrameModel is not EllipseMaskShapeKeyFrameModel ellipseKeyFrameModel)
            {
                throw new ArgumentException(nameof(keyFrameModel));
            }

            _undoRoot.BeginChangeSetBatch("Ellipse mask shape key frame copied", false);

            CenterPoint = ellipseKeyFrameModel.CenterPoint.ToWpfPoint();
            RadiusX = ellipseKeyFrameModel.RadiusX;
            RadiusY = ellipseKeyFrameModel.RadiusY;

            _undoRoot.EndChangeSetBatch();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as EllipseMaskShapeKeyFrameViewModel);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] EllipseMaskShapeKeyFrameViewModel other)
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
