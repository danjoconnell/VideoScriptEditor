using MonitoredUndo;
using Prism.Mvvm;
using System;
using System.Diagnostics.CodeAnalysis;
using VideoScriptEditor.Models;

namespace VideoScriptEditor.ViewModels.Timeline
{
    /// <summary>
    /// Base key frame view model class for coordinating interaction between a view and a <see cref="KeyFrameModelBase">key frame model</see>.
    /// </summary>
    public abstract class KeyFrameViewModelBase : BindableBase, IEquatable<KeyFrameViewModelBase>, IComparable<KeyFrameViewModelBase>, ISupportsUndo
    {
        /// <summary>The <see cref="IUndoService"/> instance providing undo/redo support.</summary>
        protected readonly IUndoService _undoService;

        /// <summary>The <see cref="IChangeFactory"/> instance for undo <see cref="Change"/> creation.</summary>
        protected readonly IChangeFactory _undoChangeFactory;

        /// <summary>The undo "root document" or "root object" for this view model.</summary>
        protected object _rootUndoObject;

        /// <summary>The <see cref="UndoRoot"/> instance for the <see cref="_rootUndoObject"/>.</summary>
        protected UndoRoot _undoRoot;

        /// <summary>Indicates whether or not this key frame is active.</summary>
        protected bool _isActive = false;

        /// <summary>
        /// The <see cref="KeyFrameModelBase">key frame model</see> providing data for consumption by a view.
        /// </summary>
        public abstract KeyFrameModelBase Model { get; protected set; }

        /// <inheritdoc cref="KeyFrameModelBase.FrameNumber"/>
        public virtual int FrameNumber
        {
            get => Model.FrameNumber;
            set
            {
                if (Model.FrameNumber != value)
                {
                    _undoChangeFactory.OnChanging(this, nameof(FrameNumber), Model.FrameNumber, value);

                    Model.FrameNumber = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates whether or not this key frame is active
        /// (the key frame at the current zero-based frame number of the video).
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>
        /// Base constructor for key frame view models derived from the <see cref="KeyFrameViewModelBase"/> class.
        /// </summary>
        /// <param name="model">The <see cref="KeyFrameModelBase">key frame model</see> providing data for consumption by a view.</param>
        /// <param name="rootUndoObject">The undo "root document" or "root object" for this view model.</param>
        /// <param name="undoService">The <see cref="IUndoService"/> instance providing undo/redo support.</param>
        /// <param name="undoChangeFactory">The <see cref="IChangeFactory"/> instance for undo <see cref="Change"/> creation.</param>
        protected KeyFrameViewModelBase(KeyFrameModelBase model, object rootUndoObject, IUndoService undoService, IChangeFactory undoChangeFactory)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            _rootUndoObject = rootUndoObject;
            _undoService = undoService;
            _undoChangeFactory = undoChangeFactory;
            _undoRoot = undoService[rootUndoObject];
        }

        /// <inheritdoc cref="ISupportsUndo.GetUndoRoot"/>
        public object GetUndoRoot() => _rootUndoObject;

        /// <summary>
        /// Linearly interpolates between this <see cref="KeyFrameViewModelBase">key frame view model</see> and another <see cref="KeyFrameViewModelBase">key frame view model</see>
        /// based on the weighting of the target frame number.
        /// </summary>
        /// <param name="targetFrameNumber">The target frame number for interpolation.</param>
        /// <param name="toKeyFrameViewModel">A <see cref="KeyFrameViewModelBase">key frame view model</see> with a <see cref="FrameNumber"/> greater than <paramref name="targetFrameNumber"/>.</param>
        /// <returns>The interpolated <see cref="KeyFrameViewModelBase">key frame view model</see>.</returns>
        public abstract KeyFrameViewModelBase Lerp(int targetFrameNumber, KeyFrameViewModelBase toKeyFrameViewModel);

        /// <summary>
        /// Sets data properties to values copied from the specified <see cref="KeyFrameModelBase">key frame model</see>.
        /// </summary>
        /// <param name="keyFrameModel">The <see cref="KeyFrameModelBase">key frame model</see> containing the data values to copy.</param>
        public abstract void CopyFromModel(KeyFrameModelBase keyFrameModel);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as SegmentViewModelBase);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] KeyFrameViewModelBase other)
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

            // If run-time types are not exactly the same, return false.
            if (GetType() != other.GetType())
            {
                return false;
            }

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return Model.Equals(other.Model);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Model);
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo(T)"/>
        public virtual int CompareTo(KeyFrameViewModelBase other)
        {
            // If other is not a valid object reference, this instance is greater. 
            if (other == null) return 1;

            return Model.CompareTo(other.Model);
        }
    }
}
