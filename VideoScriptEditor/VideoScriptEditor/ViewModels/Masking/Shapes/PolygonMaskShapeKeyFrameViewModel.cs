using MonitoredUndo;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Masking.Shapes;
using VideoScriptEditor.Models.Primitives;
using VideoScriptEditor.ViewModels.Timeline;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.ViewModels.Masking.Shapes
{
    /// <summary>
    /// View Model for coordinating interaction between a view and a <see cref="PolygonMaskShapeKeyFrameModel">polygon masking key frame model</see>.
    /// </summary>
    public class PolygonMaskShapeKeyFrameViewModel : KeyFrameViewModelBase, IEquatable<PolygonMaskShapeKeyFrameViewModel>
    {
        private PolygonMaskShapeKeyFrameModel _model;

        /// <inheritdoc cref="KeyFrameViewModelBase.Model"/>
        public override KeyFrameModelBase Model
        {
            get => _model;
            protected set => _model = (PolygonMaskShapeKeyFrameModel)value;
        }

        /// <inheritdoc cref="PolygonMaskShapeKeyFrameModel.Points"/>
        public ObservableCollection<Point> Points { get; }

        /// <summary>
        /// Creates a new <see cref="PolygonMaskShapeKeyFrameViewModel"/> instance.
        /// </summary>
        /// <param name="model">The <see cref="PolygonMaskShapeKeyFrameModel">polygon masking key frame model</see> providing data for consumption by a view.</param>
        /// <inheritdoc cref="KeyFrameViewModelBase(KeyFrameModelBase, object, IUndoService, IChangeFactory)"/>
        public PolygonMaskShapeKeyFrameViewModel(PolygonMaskShapeKeyFrameModel model, object rootUndoObject, IUndoService undoService, IChangeFactory undoChangeFactory) : base(model, rootUndoObject, undoService, undoChangeFactory)
        {
            Points = new ObservableCollection<Point>(
                model.Points.Select(pointD => pointD.ToWpfPoint())
            );

            Points.CollectionChanged += OnPointsCollectionChanged;
        }

        /// <inheritdoc/>
        public override KeyFrameViewModelBase Lerp(int fromFrameNumber, KeyFrameViewModelBase toKeyFrameViewModel)
        {
            if (toKeyFrameViewModel is not PolygonMaskShapeKeyFrameViewModel toPolygonKeyFrameViewModel)
            {
                throw new ArgumentException(nameof(toKeyFrameViewModel));
            }

            Debug.Assert(Points.Count == toPolygonKeyFrameViewModel.Points.Count, "The two polygon points counts are NOT equal!");

            int frameRange = toKeyFrameViewModel.FrameNumber - FrameNumber;
            double lerpAmount = (frameRange > 0) ? (double)(fromFrameNumber - FrameNumber) / frameRange : 0d; // prevent double.NaN value as a result of division by zero

            List<PointD> lerpedPoints = new List<PointD>(_model.Points.Count);
            for (int i = 0; i < _model.Points.Count; i++)
            {
                lerpedPoints.Add(
                    PointD.Lerp(_model.Points[i], toPolygonKeyFrameViewModel._model.Points[i], lerpAmount)
                );
            }

            return new PolygonMaskShapeKeyFrameViewModel(
                new PolygonMaskShapeKeyFrameModel(fromFrameNumber, lerpedPoints),
                _rootUndoObject, _undoService, _undoChangeFactory
            );
        }

        /// <inheritdoc/>
        public override void CopyFromModel(KeyFrameModelBase keyFrameModel)
        {
            if (keyFrameModel is not PolygonMaskShapeKeyFrameModel polygonKeyFrameModel)
            {
                throw new ArgumentException(nameof(keyFrameModel));
            }

            Debug.Assert(Points.Count == polygonKeyFrameModel.Points.Count, "The two Polygon Models Points count are NOT equal!");

            _undoRoot.BeginChangeSetBatch("Polygon mask shape key frame copied", false);

            for (int i = 0; i < Points.Count; i++)
            {
                if (!Points[i].IsEqualTo(polygonKeyFrameModel.Points[i]))
                {
                    Points[i] = polygonKeyFrameModel.Points[i].ToWpfPoint();
                }
            }

            _undoRoot.EndChangeSetBatch();
        }

        /// <summary>
        /// Invoked whenever the <see cref="Points"/> collection changes.
        /// </summary>
        /// <inheritdoc cref="NotifyCollectionChangedEventHandler"/>
        private void OnPointsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.Assert(sender == Points); // Catch memory/event handler registration leaks.

            _undoChangeFactory.OnCollectionChanged(this, nameof(Points), Points, e, "Polygon mask shape key frame Points changed");

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        Point wpfPoint = (Point)e.NewItems[i];
                        _model.Points.Insert(e.NewStartingIndex + i, wpfPoint.ToPointD());
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _model.Points.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        Point wpfPoint = (Point)e.NewItems[i];
                        _model.Points[e.NewStartingIndex + i] = wpfPoint.ToPointD();
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    _model.Points.MoveItem(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _model.Points.Clear();
                    break;
            }

            RaisePropertyChanged(nameof(Points));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as PolygonMaskShapeKeyFrameViewModel);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] PolygonMaskShapeKeyFrameViewModel other)
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
