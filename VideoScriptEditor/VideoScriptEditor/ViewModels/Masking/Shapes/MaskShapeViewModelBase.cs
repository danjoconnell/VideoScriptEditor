using MonitoredUndo;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Geometry;
using VideoScriptEditor.Models;
using VideoScriptEditor.Services;
using VideoScriptEditor.Services.ScriptVideo;
using VideoScriptEditor.ViewModels.Timeline;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.ViewModels.Masking.Shapes
{
    /// <summary>
    /// Base masking shape segment view model class for coordinating interaction between a view and a <see cref="SegmentModelBase">masking shape segment model</see>.
    /// </summary>
    public abstract class MaskShapeViewModelBase : SegmentViewModelBase, IEquatable<MaskShapeViewModelBase>
    {
        protected string ShapeResizedChangeSetDescription => $"'{Name}' masking shape resized";
        protected string ShapeKeyFrameAddedChangeSetDescription => $"'{Name}' masking shape key frame added";
        protected string ShapeKeyFrameCopiedChangeSetDescription => $"'{Name}' masking shape key frame copied";

        /// <summary>
        /// The geometric type of masking shape this view model represents.
        /// </summary>
        public abstract MaskShapeType ShapeType { get; }

        /// <summary>
        /// Gets or sets a <see cref="Rect"/> that represents the bounding box of this masking shape.
        /// </summary>
        public abstract Rect Bounds { get; set; }

        /// <summary>
        /// Base constructor for masking shape segment view models derived from the <see cref="MaskShapeViewModelBase"/> class.
        /// </summary>
        /// <inheritdoc cref="SegmentViewModelBase(SegmentModelBase, IScriptVideoContext, object, IUndoService, IChangeFactory, IClipboardService, KeyFrameViewModelCollection)"/>
        protected MaskShapeViewModelBase(SegmentModelBase model, IScriptVideoContext scriptVideoContext, object rootUndoObject, IUndoService undoService, IChangeFactory undoChangeFactory, IClipboardService clipboardService, KeyFrameViewModelCollection keyFrameViewModels = null) : base(model, scriptVideoContext, rootUndoObject, undoService, undoChangeFactory, clipboardService, keyFrameViewModels)
        {
        }

        /// <summary>
        /// Determines whether this masking shape supports the
        /// specified <see cref="MaskShapeResizeMode">resize mode</see>
        /// </summary>
        /// <param name="resizeMode">A <see cref="MaskShapeResizeMode"/> enum value representing the resize mode to check.</param>
        /// <returns><see langword="true"/> if this masking shape supports the specified resize mode; otherwise, <see langword="false"/>.</returns>
        public abstract bool SupportsResizeMode(MaskShapeResizeMode resizeMode);

        /// <summary>
        /// Resizes the bounding box of the masking shape from the specified origin point.
        /// </summary>
        /// <param name="newBounds">A <see cref="Rect"/> specifying the new bounding size and location for this masking shape.</param>
        /// <param name="resizeOrigin">A <see cref="RectanglePoint"/> enum value representing the resize origin point. Can be <see langword="null"/>.</param>
        public abstract void ResizeBounds(Rect newBounds, RectanglePoint? resizeOrigin = null);

        /// <summary>
        /// Begins an undoable action making <see cref="UndoRoot.BeginChangeSetBatch(string, bool)">batch changes</see>
        /// to the location of the masking shape.
        /// </summary>
        public virtual void BeginShapeMoveAction()
        {
            _undoRoot.BeginChangeSetBatch($"{Name} mask shape moved", true);
        }

        /// <summary>
        /// Begins an undoable action making <see cref="UndoRoot.BeginChangeSetBatch(string, bool)">batch changes</see>
        /// to the size of the masking shape.
        /// </summary>
        public virtual void BeginShapeResizeAction()
        {
            _undoRoot.BeginChangeSetBatch($"{Name} mask shape resized", true);
        }

        /// <summary>
        /// Moves the bounding box of the masking shape by the specified <see cref="Vector"/>.
        /// </summary>
        /// <param name="offsetVector">
        /// A <see cref="Vector"/> that specifies the horizontal and vertical amounts to move the bounding box.
        /// </param>
        public abstract void OffsetBounds(Vector offsetVector);

        /// <summary>
        /// Moves the bounding box of the masking shape by the specified <see cref="Vector"/>
        /// within the <see cref="IScriptVideoContext.VideoFrameSize">video frame bounds</see>.
        /// </summary>
        /// <param name="offsetVector">
        ///  A <see cref="Vector"/> that specifies the horizontal and vertical amounts to move the bounding box.
        /// </param>
        public void OffsetWithinVideoFrameBounds(Vector offsetVector)
        {
            if (!CanBeEdited)
            {
                return;
            }

            Rect videoFrameBounds = new Rect(ScriptVideoContext.VideoFrameSize.ToWpfSize());
            Rect currentBounds = Bounds;
            Rect offsetBounds = currentBounds.OffsetFromCenterWithinBounds(offsetVector, videoFrameBounds);

            OffsetBounds(offsetBounds.Location - currentBounds.Location);
        }

        /// <summary>
        /// Resizes the bounding box of the masking shape by offsetting a bounding point,
        /// optionally using a scaled resize method.
        /// </summary>
        /// <param name="boundingBoxPoint">A <see cref="RectanglePoint"/> enum value representing the bounding point to offset.</param>
        /// <param name="boundingPointOffset">The pixel offset to apply to the bounding point.</param>
        /// <param name="scaledResize">Whether to use a scaled resize method. Defaults to <see langword="false"/>.</param>
        public void ResizeFromBoundingPointOffset(RectanglePoint boundingBoxPoint, Vector boundingPointOffset, bool scaledResize = false)
        {
            if (!CanBeEdited)
            {
                return;
            }

            Rect videoBounds = new Rect(ScriptVideoContext.VideoFrameSize.ToWpfSize());
            Rect shapeBounds = Bounds;
            Point centerPoint = shapeBounds.CenterPoint();

            double left = shapeBounds.Left, top = shapeBounds.Top, right = shapeBounds.Right, bottom = shapeBounds.Bottom;
            double width = shapeBounds.Width, height = shapeBounds.Height, scaleWidth = 1d, scaleHeight = 1d;
            Point originalPoint, projectedPoint;
            Line projectionLine;
            double xOffset, xOffsetMax, yOffset, yOffsetMax;

            switch (boundingBoxPoint)
            {
                case RectanglePoint.TopLeft:
                    originalPoint = shapeBounds.TopLeft;
                    projectedPoint = videoBounds.ConstrainPoint(originalPoint + boundingPointOffset);
                    if (scaledResize)
                    {
                        if (projectedPoint == originalPoint || projectedPoint.X > centerPoint.X || projectedPoint.Y > centerPoint.Y)
                        {
                            break;
                        }

                        projectionLine = centerPoint.LineTo(originalPoint);
                        projectedPoint = projectionLine.Project(projectedPoint);
                        if (!videoBounds.Contains(projectedPoint))
                        {
                            break;
                        }
                    }

                    left = projectedPoint.X;
                    top = projectedPoint.Y;
                    break;
                case RectanglePoint.TopCenter:
                    originalPoint = shapeBounds.TopCenter();
                    projectedPoint = videoBounds.ConstrainPoint(new Point(originalPoint.X, originalPoint.Y + boundingPointOffset.Y));
                    if (scaledResize)
                    {
                        if (projectedPoint == originalPoint || projectedPoint.Y > centerPoint.Y)
                        {
                            break;
                        }

                        height = shapeBounds.Bottom - projectedPoint.Y;
                        scaleHeight = height / shapeBounds.Height;
                        width = shapeBounds.Width * scaleHeight;
                        xOffset = (width - shapeBounds.Width) / 2d;
                        if (height > shapeBounds.Height)
                        {
                            // Upscaling

                            if (shapeBounds.Left == videoBounds.Left || shapeBounds.Right == videoBounds.Right)
                            {
                                // Can't expand width any further
                                break;
                            }

                            xOffsetMax = Math.Min(shapeBounds.Left, videoBounds.Right - shapeBounds.Right);
                            if (xOffset > xOffsetMax)
                            {
                                // Scale by width
                                width = shapeBounds.Width + (xOffsetMax * 2d);
                                scaleWidth = width / shapeBounds.Width;
                                height = shapeBounds.Height * scaleWidth;

                                projectedPoint.Y = shapeBounds.Bottom - height;
                                xOffset = xOffsetMax;
                            }
                        }

                        left = shapeBounds.Left - xOffset;
                        right = shapeBounds.Right + xOffset;
                    }

                    top = projectedPoint.Y;
                    break;
                case RectanglePoint.TopRight:
                    originalPoint = shapeBounds.TopRight;
                    projectedPoint = videoBounds.ConstrainPoint(originalPoint + boundingPointOffset);
                    if (scaledResize)
                    {
                        if (projectedPoint == originalPoint || projectedPoint.X < centerPoint.X || projectedPoint.Y > centerPoint.Y)
                        {
                            break;
                        }

                        projectionLine = centerPoint.LineTo(originalPoint);
                        projectedPoint = projectionLine.Project(projectedPoint);
                        if (!videoBounds.Contains(projectedPoint))
                        {
                            break;
                        }
                    }

                    right = projectedPoint.X;
                    top = projectedPoint.Y;
                    break;
                case RectanglePoint.CenterLeft:
                    originalPoint = shapeBounds.CenterLeft();
                    projectedPoint = videoBounds.ConstrainPoint(new Point(originalPoint.X + boundingPointOffset.X, originalPoint.Y));
                    if (scaledResize)
                    {
                        if (projectedPoint == originalPoint || projectedPoint.X > centerPoint.X)
                        {
                            break;
                        }

                        width = shapeBounds.Right - projectedPoint.X;
                        scaleWidth = width / shapeBounds.Width;
                        height = shapeBounds.Height * scaleWidth;
                        yOffset = (height - shapeBounds.Height) / 2d;
                        if (width > shapeBounds.Width)
                        {
                            // Upscaling

                            if (shapeBounds.Top == videoBounds.Top || shapeBounds.Bottom == videoBounds.Bottom)
                            {
                                // Can't expand height any further
                                break;
                            }

                            yOffsetMax = Math.Min(shapeBounds.Top, videoBounds.Bottom - shapeBounds.Bottom);
                            if (yOffset > yOffsetMax)
                            {
                                // Scale by height
                                height = shapeBounds.Height + (yOffsetMax * 2d);
                                scaleHeight = height / shapeBounds.Height;
                                width = shapeBounds.Width * scaleHeight;

                                projectedPoint.X = shapeBounds.Right - width;
                                yOffset = yOffsetMax;
                            }
                        }

                        top = shapeBounds.Top - yOffset;
                        bottom = shapeBounds.Bottom + yOffset;
                    }

                    left = projectedPoint.X;
                    break;
                case RectanglePoint.CenterRight:
                    originalPoint = shapeBounds.CenterRight();
                    projectedPoint = videoBounds.ConstrainPoint(new Point(originalPoint.X + boundingPointOffset.X, originalPoint.Y));
                    if (scaledResize)
                    {
                        if (projectedPoint == originalPoint || projectedPoint.X < centerPoint.X)
                        {
                            break;
                        }

                        width = projectedPoint.X - shapeBounds.Left;
                        scaleWidth = width / shapeBounds.Width;
                        height = shapeBounds.Height * scaleWidth;
                        yOffset = (height - shapeBounds.Height) / 2d;
                        if (width > shapeBounds.Width)
                        {
                            // Upscaling

                            if (shapeBounds.Top == videoBounds.Top || shapeBounds.Bottom == videoBounds.Bottom)
                            {
                                // Can't expand height any further
                                break;
                            }

                            yOffsetMax = Math.Min(shapeBounds.Top, videoBounds.Bottom - shapeBounds.Bottom);
                            if (yOffset > yOffsetMax)
                            {
                                // Scale by height
                                height = shapeBounds.Height + (yOffsetMax * 2d);
                                scaleHeight = height / shapeBounds.Height;
                                width = shapeBounds.Width * scaleHeight;

                                projectedPoint.X = shapeBounds.Left + width;
                                yOffset = yOffsetMax;
                            }
                        }

                        top = shapeBounds.Top - yOffset;
                        bottom = shapeBounds.Bottom + yOffset;
                    }

                    right = projectedPoint.X;
                    break;
                case RectanglePoint.BottomLeft:
                    originalPoint = shapeBounds.BottomLeft;
                    projectedPoint = videoBounds.ConstrainPoint(originalPoint + boundingPointOffset);
                    if (scaledResize)
                    {
                        if (projectedPoint == originalPoint || projectedPoint.X > centerPoint.X || projectedPoint.Y < centerPoint.Y)
                        {
                            break;
                        }

                        projectionLine = centerPoint.LineTo(originalPoint);
                        projectedPoint = projectionLine.Project(projectedPoint);
                        if (!videoBounds.Contains(projectedPoint))
                        {
                            break;
                        }
                    }

                    left = projectedPoint.X;
                    bottom = projectedPoint.Y;
                    break;
                case RectanglePoint.BottomCenter:
                    originalPoint = shapeBounds.BottomCenter();
                    projectedPoint = videoBounds.ConstrainPoint(new Point(originalPoint.X, originalPoint.Y + boundingPointOffset.Y));
                    if (scaledResize)
                    {
                        if (projectedPoint == originalPoint || projectedPoint.Y < centerPoint.Y)
                        {
                            break;
                        }

                        height = projectedPoint.Y - shapeBounds.Top;
                        scaleHeight = height / shapeBounds.Height;
                        width = shapeBounds.Width * scaleHeight;
                        xOffset = (width - shapeBounds.Width) / 2d;
                        if (height > shapeBounds.Height)
                        {
                            // Upscaling

                            if (shapeBounds.Left == videoBounds.Left || shapeBounds.Right == videoBounds.Right)
                            {
                                // Can't expand width any further
                                break;
                            }

                            xOffsetMax = Math.Min(shapeBounds.Left, videoBounds.Right - shapeBounds.Right);
                            if (xOffset > xOffsetMax)
                            {
                                // Scale by width
                                width = shapeBounds.Width + (xOffsetMax * 2d);
                                scaleWidth = width / shapeBounds.Width;
                                height = shapeBounds.Height * scaleWidth;

                                projectedPoint.Y = shapeBounds.Top + height;
                                xOffset = xOffsetMax;
                            }
                        }

                        left = shapeBounds.Left - xOffset;
                        right = shapeBounds.Right + xOffset;
                    }

                    bottom = projectedPoint.Y;
                    break;
                case RectanglePoint.BottomRight:
                    originalPoint = shapeBounds.BottomRight;
                    projectedPoint = videoBounds.ConstrainPoint(originalPoint + boundingPointOffset);
                    if (scaledResize)
                    {
                        if (projectedPoint == originalPoint || projectedPoint.X < centerPoint.X || projectedPoint.Y < centerPoint.Y)
                        {
                            break;
                        }

                        projectionLine = centerPoint.LineTo(originalPoint);
                        projectedPoint = projectionLine.Project(projectedPoint);
                        if (!videoBounds.Contains(projectedPoint))
                        {
                            break;
                        }
                    }

                    right = projectedPoint.X;
                    bottom = projectedPoint.Y;
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(boundingBoxPoint));
            }

            shapeBounds = new Rect(new Point(left, top),
                                   new Point(right, bottom));
#if DEBUG
            if (scaledResize)
            {
                Debug.Assert((float)(shapeBounds.Width / Bounds.Width) == (float)(shapeBounds.Height / Bounds.Height));
            }
#endif
            ResizeBounds(shapeBounds, boundingBoxPoint);
        }

        /// <summary>
        /// Flips the masking shape along the specified axis.
        /// </summary>
        /// <param name="axis">The axis to flip the masking shape along.</param>
        public abstract void Flip(Axis axis);

        /// <inheritdoc/>
        protected override void OnActiveKeyFrameChanged()
        {
            base.OnActiveKeyFrameChanged();

            OnDataPropertyValuesChanged();
        }

        /// <summary>
        /// Invoked when all data property values have changed.
        /// Raises <see cref="INotifyPropertyChanged.PropertyChanged"/> for each property.
        /// </summary>
        protected abstract void OnDataPropertyValuesChanged();

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event for the <see cref="Bounds"/> property.
        /// </summary>
        protected void RaiseBoundsPropertyChanged() => RaisePropertyChanged(nameof(Bounds));

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as MaskShapeViewModelBase);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] MaskShapeViewModelBase other)
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
            return ShapeType == other.ShapeType && base.Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), ShapeType);
        }
    }
}
