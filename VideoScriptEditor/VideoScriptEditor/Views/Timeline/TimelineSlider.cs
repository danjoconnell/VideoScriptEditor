using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.Views.Timeline
{
    /// <summary>
    /// A custom <see cref="Slider"/> for displaying the current video frame position
    /// in the timeline or navigating to a specific frame by moving a <see cref="Track.Thumb"/> control along a <see cref="Track"/>.
    /// </summary>
    [TemplatePart(Name = TopTickBarElementName, Type = typeof(TickBar))]
    [TemplatePart(Name = BottomTickBarElementName, Type = typeof(TickBar))]
    public class TimelineSlider : Slider
    {
        private const string TopTickBarElementName = "PART_TopTickBar";
        private const string BottomTickBarElementName = "PART_BottomTickBar";

        /// <summary>
        /// Identifies the <see cref="ParentScrollViewer" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty ParentScrollViewerProperty = DependencyProperty.Register(
            nameof(ParentScrollViewer),
            typeof(ScrollViewer),
            typeof(TimelineSlider),
            new FrameworkPropertyMetadata(null, OnParentScrollViewerPropertyChanged));

        /// <summary>
        /// Gets or sets the parent <see cref="ScrollViewer"/> element.
        /// </summary>
        /// <remarks>
        /// Can be a name reference.
        /// </remarks>
        /// <example>
        /// <c>&lt;TimelineSlider ParentScrollViewer="myScrollViewer" /&gt;</c>
        /// </example>
        [TypeConverter(typeof(NameReferenceConverter))]
        public ScrollViewer ParentScrollViewer
        {
            get => (ScrollViewer)GetValue(ParentScrollViewerProperty);
            set => SetValue(ParentScrollViewerProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TickSpacing" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="TickSpacing" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty TickSpacingProperty = DependencyProperty.Register(
            nameof(TickSpacing),
            typeof(double),
            typeof(TimelineSlider),
            new FrameworkPropertyMetadata(8d));

        /// <inheritdoc cref="TimelineTickBar.TickSpacing"/>
        public double TickSpacing
        {
            get => (double)GetValue(TickSpacingProperty);
            set => SetValue(TickSpacingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TickNumberLabelFrequency" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickNumberLabelFrequencyProperty = DependencyProperty.Register(
            nameof(TickNumberLabelFrequency),
            typeof(uint),
            typeof(TimelineSlider),
            new FrameworkPropertyMetadata(5u));

        /// <inheritdoc cref="TimelineTickBar.TickNumberLabelFrequency"/>
        [Bindable(true), Category("Appearance")]
        public uint TickNumberLabelFrequency
        {
            get => (uint)GetValue(TickNumberLabelFrequencyProperty);
            set => SetValue(TickNumberLabelFrequencyProperty, value);
        }

        /// <summary>
        /// Creates a new <see cref="TimelineSlider"/> instance.
        /// </summary>
        public TimelineSlider() : base()
        {
        }

        /// <summary>
        /// The static constructor for the <see cref="TimelineSlider"/> class.
        /// It simply defines a default style sheet.
        /// </summary>
        static TimelineSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimelineSlider), new FrameworkPropertyMetadata(typeof(TimelineSlider)));
        }

        /// <summary>
        /// Gets or sets a reference to the Top <see cref="TickBar"/> element.
        /// </summary>
        protected TickBar TopTickBarElement { get; set; }

        /// <summary>
        /// Gets or sets a reference to the Bottom <see cref="TickBar"/> element.
        /// </summary>
        protected TickBar BottomTickBarElement { get; set; }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            TopTickBarElement = GetTemplateChild(TopTickBarElementName) as TickBar;
            Debug.Assert(TopTickBarElement != null);

            BottomTickBarElement = GetTemplateChild(BottomTickBarElementName) as TickBar;
            Debug.Assert(BottomTickBarElement != null);
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="ParentScrollViewer"/> property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnParentScrollViewerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimelineSlider sliderInstance = (TimelineSlider)d;

            if (e.OldValue is ScrollViewer oldParentScrollViewer)
            {
                oldParentScrollViewer.ScrollChanged -= sliderInstance.OnParentScrollViewerScrollChanged;
            }

            if (e.NewValue is ScrollViewer newParentScrollViewer)
            {
                newParentScrollViewer.ScrollChanged -= sliderInstance.OnParentScrollViewerScrollChanged;
                newParentScrollViewer.ScrollChanged += sliderInstance.OnParentScrollViewerScrollChanged;
            }
        }

        /// <summary>
        /// Handles the <see cref="ScrollViewer.ScrollChanged"/> routed event for the <see cref="ParentScrollViewer"/> element.
        /// </summary>
        /// <inheritdoc cref="ScrollChangedEventHandler"/>
        private void OnParentScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange != 0d)
            {
                switch (TickPlacement)
                {
                    case TickPlacement.TopLeft:
                        TopTickBarElement?.InvalidateVisual();
                        break;
                    case TickPlacement.BottomRight:
                        BottomTickBarElement?.InvalidateVisual();
                        break;
                    case TickPlacement.Both:
                        TopTickBarElement?.InvalidateVisual();
                        BottomTickBarElement?.InvalidateVisual();
                        break;
                }
            }
        }
    }
}
