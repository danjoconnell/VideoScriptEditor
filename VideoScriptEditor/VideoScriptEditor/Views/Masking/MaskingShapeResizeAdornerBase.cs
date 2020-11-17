using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using VideoScriptEditor.ViewModels.Masking;
using VideoScriptEditor.ViewModels.Masking.Shapes;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.Views.Masking
{
    /// <summary>
    /// Base class for masking shape resize <see cref="Adorner"/>s.
    /// </summary>
    public abstract class MaskingShapeResizeAdornerBase : Adorner
    {
        /// <summary>Stores and manages the adorner's visual children.</summary>
        protected readonly VisualCollection _visualChildren;

        /// <inheritdoc cref="BooleanToVisibilityConverter"/>
        protected readonly BooleanToVisibilityConverter _booleanToVisibilityConverter = new BooleanToVisibilityConverter();

        /// <inheritdoc cref="IMaskingViewModel"/>
        protected IMaskingViewModel MaskingViewModel => ((FrameworkElement)AdornedElement).DataContext as IMaskingViewModel;

        /// <summary>
        /// Base constructor for masking shape resize adorners derived from the <see cref="MaskingShapeResizeAdornerBase"/> class.
        /// </summary>
        /// <inheritdoc cref="Adorner(UIElement)"/>
        protected MaskingShapeResizeAdornerBase(FrameworkElement adornedElement) : base(adornedElement)
        {
            Debug.Assert(adornedElement.DataContext is IMaskingViewModel, "IMaskingViewModel instance was null!");

            _visualChildren = new VisualCollection(this);
        }

        /// <inheritdoc/>
        /// <remarks>Overridden to interface with the adorner's visual collection.</remarks>
        protected override int VisualChildrenCount => _visualChildren.Count;

        /// <inheritdoc/>
        /// <remarks>Overridden to interface with the adorner's visual collection.</remarks>
        protected override Visual GetVisualChild(int index) => _visualChildren[index];

        /// <summary>
        /// Creates a handle <see cref="Thumb"/>, sets the <see cref="Cursor"/>
        /// and appearance properties, and optionally stores a value in the <see cref="FrameworkElement.Tag">Tag</see>.
        /// </summary>
        /// <param name="cursor">The cursor to display when the mouse pointer is over the <see cref="Thumb"/>.</param>
        /// <param name="tagData">
        /// A value to store in the <see cref="Thumb"/>'s <see cref="FrameworkElement.Tag">Tag</see> property.
        /// Defaults to null.
        /// </param>
        /// <returns>A new <see cref="Thumb"/> instance.</returns>
        protected Thumb CreateHandleThumb(Cursor cursor, object tagData = null)
        {
            IMaskingViewModel maskingViewModel = MaskingViewModel;

            Thumb handleThumb = new Thumb()
            {
                // Set some arbitrary visual characteristics.
                Cursor = cursor,
                Height = 10,
                Width = 10,
                Opacity = 0.40,
                Background = new SolidColorBrush(Colors.MediumBlue),
                Tag = tagData
            };

            handleThumb.SetBinding(
                IsEnabledProperty,
                new Binding($"{nameof(IMaskingViewModel.SelectedSegment)}.{nameof(MaskShapeViewModelBase.CanBeEdited)}")
                {
                    Source = maskingViewModel,
                    Mode = BindingMode.OneWay,
                    FallbackValue = false
                }
            );

            handleThumb.SetBinding(
                VisibilityProperty,
                new Binding($"{nameof(IMaskingViewModel.SelectedSegment)}.{nameof(MaskShapeViewModelBase.CanBeEdited)}")
                {
                    Source = maskingViewModel,
                    Mode = BindingMode.OneWay,
                    Converter = _booleanToVisibilityConverter,
                    FallbackValue = Visibility.Collapsed
                }
            );

            handleThumb.DragStarted += OnHandleThumbDragStarted;
            handleThumb.DragDelta += OnHandleThumbDragDelta;
            handleThumb.DragCompleted += OnHandleThumbDragCompleted;

            return handleThumb;
        }

        /// <summary>
        /// Arranges a handle <see cref="Thumb"/>
        /// so that it is centered in the specified <see cref="Point">position</see>.
        /// </summary>
        /// <param name="handleThumb">The handle <see cref="Thumb"/> to arrange.</param>
        /// <param name="position">
        /// A <see cref="Point"/> specifying the center position to arrange the <see cref="Thumb"/>.
        /// </param>
        protected void ArrangeHandleThumb(Thumb handleThumb, Point position)
        {
            Vector halfSize = (Vector)handleThumb.DesiredSize / 2;
            handleThumb.Arrange(new Rect(position - halfSize, position + halfSize));
        }

        /// <summary>
        /// Handles the <see cref="Thumb.DragStarted"/> event for a handle <see cref="Thumb"/>.
        /// </summary>
        /// <inheritdoc cref="DragStartedEventHandler"/>
        protected void OnHandleThumbDragStarted(object sender, DragStartedEventArgs e)
        {
            (MaskingViewModel.SelectedSegment as MaskShapeViewModelBase)?.BeginShapeResizeAction();
        }

        /// <summary>
        /// Handles the <see cref="Thumb.DragDelta"/> event for a handle <see cref="Thumb"/>.
        /// </summary>
        /// <inheritdoc cref="DragDeltaEventHandler"/>
        protected abstract void OnHandleThumbDragDelta(object sender, DragDeltaEventArgs e);

        /// <summary>
        /// Handles the <see cref="Thumb.DragCompleted"/> event for a handle <see cref="Thumb"/>.
        /// </summary>
        /// <inheritdoc cref="DragCompletedEventHandler"/>
        protected void OnHandleThumbDragCompleted(object sender, DragCompletedEventArgs e)
        {
            MaskingViewModel.SelectedSegment?.CompleteUndoableAction();
        }
    }
}
