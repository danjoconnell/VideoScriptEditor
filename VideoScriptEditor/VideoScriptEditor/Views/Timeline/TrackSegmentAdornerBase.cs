using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using VideoScriptEditor.ViewModels.Timeline;

namespace VideoScriptEditor.Views.Timeline
{
    /// <summary>
    /// Base class for timeline track segment <see cref="Adorner"/>s.
    /// </summary>
    public abstract class TrackSegmentAdornerBase : Adorner
    {
        /// <summary>The <see cref="AdornerLayer"/> for rendering this <see cref="Adorner"/>.</summary>
        protected readonly AdornerLayer _adornerLayer;

        /// <summary><see cref="Border"/> providing a visual preview of changes to the location and size of a <see cref="VideoTimelineSegment"/>.</summary>
        /// <remarks>This <see cref="Adorner"/>'s visual child element.</remarks>
        protected readonly Border _previewBorder;

        /// <summary>The <see cref="ToolTip"/> that is displayed for this <see cref="Adorner"/>.</summary>
        protected readonly ToolTip _previewToolTip;

        /// <summary>
        /// The data presented in the <see cref="FrameworkElement.ToolTip">ToolTip</see>.
        /// </summary>
        public TrackSegmentAdornerToolTipViewModel ToolTipData { get; }

        /// <summary>
        /// Base constructor for timeline track segment <see cref="Adorner"/>s derived from the <see cref="TrackSegmentAdornerBase"/> class.
        /// </summary>
        /// <inheritdoc cref="Adorner(UIElement)"/>
        protected TrackSegmentAdornerBase(UIElement adornedElement) : base(adornedElement)
        {
            _adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);

            ToolTipData = new TrackSegmentAdornerToolTipViewModel();

            _previewToolTip = new ToolTip()
            {
                Content = ToolTipData,
                PlacementTarget = adornedElement,
                Placement = PlacementMode.Relative,
                StaysOpen = true
            };
            ToolTip = _previewToolTip;

            _previewBorder = new Border();
        }

        /// <summary>
        /// Attaches this <see cref="Adorner"/> to the adorned element.
        /// </summary>
        public void Attach()
        {
            // Check if already adorning.
            Adorner[] adorners = _adornerLayer.GetAdorners(AdornedElement);
            if (adorners == null || !adorners.Contains(this))
            {
                // Adorn.
                _adornerLayer.Add(this);
            }

            _previewToolTip.IsOpen = true;
        }

        /// <summary>
        /// Detaches this <see cref="Adorner"/> from the adorned element.
        /// </summary>
        public void Detach()
        {
            _previewToolTip.IsOpen = false;
            _adornerLayer.Remove(this);
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size constraint)
        {
            _previewBorder.Measure(constraint);
            return _previewBorder.DesiredSize;
        }

        /// <inheritdoc/>
        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _previewBorder;
        }

        /// <inheritdoc/>
        protected override int VisualChildrenCount => 1;
    }
}
