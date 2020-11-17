/*
    Adapted from https://github.com/dotnet/wpf/blob/master/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/Primitives/TickBar.cs
    Font Dependency Properties adapted from https://github.com/dotnet/wpf/blob/master/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/Control.cs
    Licensed to the .NET Foundation under one or more agreements.
    The .NET Foundation licenses this file to you under the MIT license.
    See https://github.com/dotnet/wpf/blob/master/LICENSE.TXT for more information.
*/

using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using DoubleUtil = VideoScriptEditor.Geometry.DoubleUtil;

namespace VideoScriptEditor.Views.Timeline
{
    /// <summary>
    /// Describes the type of tick to be rendered in a <see cref="TimelineTickBar"/>.
    /// </summary>
    public enum TickBarTickType
    {
        Lines,
        Numbers
    }

    /// <summary>
    /// A custom <see cref="TickBar"/> that draws a set of lined or numbered tick marks
    /// at a specific pixel spaced interval along a <see cref="Slider"/> control.
    /// </summary>
    public class TimelineTickBar : TickBar
    {
        private TimelineSlider _parentSliderControl;

        /// <summary>
        /// Identifies the <see cref="FontFamily"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontFamilyProperty =
                TextElement.FontFamilyProperty.AddOwner(
                        typeof(TimelineTickBar),
                        new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily,
                            FrameworkPropertyMetadataOptions.Inherits, OnSizeDeterminingPropertyChanged));

        /// <inheritdoc cref="Control.FontFamily"/>
        [Bindable(true), Category("Appearance")]
        [Localizability(LocalizationCategory.Font)]
        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontSizeProperty =
                TextElement.FontSizeProperty.AddOwner(
                        typeof(TimelineTickBar),
                        new FrameworkPropertyMetadata(SystemFonts.MessageFontSize,
                            FrameworkPropertyMetadataOptions.Inherits, OnSizeDeterminingPropertyChanged));

        /// <inheritdoc cref="Control.FontSize"/>
        [TypeConverter(typeof(FontSizeConverter))]
        [Bindable(true), Category("Appearance")]
        [Localizability(LocalizationCategory.None)]
        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FontStretch"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontStretchProperty
            = TextElement.FontStretchProperty.AddOwner(typeof(TimelineTickBar),
                    new FrameworkPropertyMetadata(TextElement.FontStretchProperty.DefaultMetadata.DefaultValue,
                        FrameworkPropertyMetadataOptions.Inherits, OnSizeDeterminingPropertyChanged));

        /// <inheritdoc cref="Control.FontStretch"/>
        [Bindable(true), Category("Appearance")]
        public FontStretch FontStretch
        {
            get => (FontStretch)GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FontStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontStyleProperty =
                TextElement.FontStyleProperty.AddOwner(
                        typeof(TimelineTickBar),
                        new FrameworkPropertyMetadata(SystemFonts.MessageFontStyle,
                            FrameworkPropertyMetadataOptions.Inherits, OnSizeDeterminingPropertyChanged));

        /// <inheritdoc cref="Control.FontStyle"/>
        [Bindable(true), Category("Appearance")]
        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FontWeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontWeightProperty =
                TextElement.FontWeightProperty.AddOwner(
                        typeof(TimelineTickBar),
                        new FrameworkPropertyMetadata(SystemFonts.MessageFontWeight,
                            FrameworkPropertyMetadataOptions.Inherits, OnSizeDeterminingPropertyChanged));

        /// <inheritdoc cref="Control.FontWeight"/>
        [Bindable(true), Category("Appearance")]
        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TickType" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickTypeProperty = DependencyProperty.Register(
            nameof(TickType),
            typeof(TickBarTickType),
            typeof(TimelineTickBar),
            new FrameworkPropertyMetadata(TickBarTickType.Lines));

        /// <summary>
        /// Gets or sets the <see cref="TickBarTickType">type</see> of tick to be rendered.
        /// </summary>
        public TickBarTickType TickType
        {
            get => (TickBarTickType)GetValue(TickTypeProperty);
            set => SetValue(TickTypeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TickSpacing" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickSpacingProperty = DependencyProperty.Register(
            nameof(TickSpacing),
            typeof(double),
            typeof(TimelineTickBar),
            new FrameworkPropertyMetadata(8d));

        /// <summary>
        /// Gets or sets the number of pixels between tick marks.
        /// </summary>
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
            typeof(TimelineTickBar),
            new FrameworkPropertyMetadata(5u, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the number of ticks between number labels.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public uint TickNumberLabelFrequency
        {
            get => (uint)GetValue(TickNumberLabelFrequencyProperty);
            set => SetValue(TickNumberLabelFrequencyProperty, value);
        }

        /// <summary>
        /// Creates a new <see cref="TimelineTickBar"/> instance.
        /// </summary>
        public TimelineTickBar() : base()
        {
        }

        /// <summary>
        /// The static constructor for the <see cref="TimelineTickBar"/> class.
        /// </summary>
        static TimelineTickBar()
        {
            MaximumProperty.OverrideMetadata(
                typeof(TimelineTickBar),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender, OnSizeDeterminingPropertyChanged));
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for properties that determine the size of the control.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnSizeDeterminingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimelineTickBar tickBarInstance = (TimelineTickBar)d;

            if (tickBarInstance.TickType == TickBarTickType.Numbers)
            {
                tickBarInstance.FitTickbarHeightToFontSize();
            }
        }

        /// <inheritdoc/>
        protected override void OnRender(DrawingContext dc)
        {
            Size size = new Size(ActualWidth, ActualHeight);
            double primaryTickLen;         // Height for Primary Tick (Minimum and Maximum value)
            double secondaryTickLen;       // Height for Secondary Tick
            Point startPoint;
            Point endPoint;

            // Take Thumb size in to account
            double halfReservedSpace = ReservedSpace * 0.5;

            switch (Placement)
            {
                case TickBarPlacement.Top:
                    if (DoubleUtil.GreaterThanOrClose(ReservedSpace, size.Width))
                    {
                        return;
                    }
                    size.Width -= ReservedSpace;
                    primaryTickLen = -size.Height;
                    startPoint = TickType == TickBarTickType.Numbers ? new Point(halfReservedSpace, 0d) : new Point(halfReservedSpace, size.Height);
                    endPoint = new Point(halfReservedSpace + size.Width, size.Height);
                    break;

                case TickBarPlacement.Bottom:
                    if (DoubleUtil.GreaterThanOrClose(ReservedSpace, size.Width))
                    {
                        return;
                    }
                    size.Width -= ReservedSpace;
                    primaryTickLen = size.Height;
                    startPoint = new Point(halfReservedSpace, 0d);
                    endPoint = new Point(halfReservedSpace + size.Width, 0d);
                    break;

                default:
                    throw new NotImplementedException($"Support for {Placement} Placement not implemented");
            };

            secondaryTickLen = primaryTickLen * 0.75;

            Pen pen = new Pen(Fill, 1.0d);

            if (TickType == TickBarTickType.Lines)
            {
                // Draw Min & Max tick
                dc.DrawLine(pen, startPoint, new Point(startPoint.X, startPoint.Y + primaryTickLen));
                dc.DrawLine(pen, new Point(endPoint.X, startPoint.Y),
                                 new Point(endPoint.X, startPoint.Y + primaryTickLen));
            }

            double tickSpacing = TickSpacing;
            double pixelsPerFrame = endPoint.X / Maximum;
            if (pixelsPerFrame > tickSpacing)
            {
                tickSpacing = pixelsPerFrame;
            }

            ScrollViewer parentScrollViewer = _parentSliderControl.ParentScrollViewer;

            double tickDrawStartX = startPoint.X + Math.Max(0d, parentScrollViewer.HorizontalOffset - VisualOffset.X);
            double tickDrawEndX = parentScrollViewer.ViewportWidth > 0d
                                    ? Math.Min(tickDrawStartX + parentScrollViewer.ViewportWidth, endPoint.X)
                                    : endPoint.X;

            uint startTickNum = (uint)Math.Floor(tickDrawStartX / tickSpacing);
            uint endTickNum = (uint)Math.Floor(tickDrawEndX / tickSpacing);

            if (TickType == TickBarTickType.Numbers)
            {
                uint numberFrequency = TickNumberLabelFrequency;

                if (startTickNum >= numberFrequency)
                {
                    //https://www.quora.com/How-do-I-get-the-next-multiple-of-5-of-a-number-in-C
                    startTickNum += numberFrequency - (startTickNum % numberFrequency);
                }
                else
                {
                    startTickNum = numberFrequency;
                }

                endTickNum -= endTickNum % numberFrequency;

                Typeface typeFace = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
                CultureInfo cultureInfo = CultureInfo.CurrentCulture;
                double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                FormattedText formattedText;

                for (uint i = startTickNum; i <= endTickNum; i += numberFrequency)
                {
                    double x = i * tickSpacing;
                    formattedText = new FormattedText((x / pixelsPerFrame).ToString("F0"), cultureInfo,
                                                      FlowDirection.LeftToRight, typeFace, FontSize, Fill, pixelsPerDip);

                    dc.DrawText(formattedText, new Point(x + startPoint.X, startPoint.Y));
                }
            }
            else if (TickType == TickBarTickType.Lines)
            {
                if (startTickNum == 0 && endTickNum > 0)
                {
                    startTickNum = 1;
                }

                for (uint i = startTickNum; i <= endTickNum; i++)
                {
                    double x = i * tickSpacing + startPoint.X;
                    dc.DrawLine(pen,
                        new Point(x, startPoint.Y),
                        new Point(x, startPoint.Y + secondaryTickLen));
                }
            }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            _parentSliderControl = TemplatedParent as TimelineSlider;

            return base.MeasureOverride(availableSize);
        }

        /// <summary>
        /// Adjusts the <see cref="MinHeight"/> so that the largest numbered tick to be rendered can fit.
        /// </summary>
        private void FitTickbarHeightToFontSize()
        {
            Typeface typeFace = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            FormattedText textSizingSample = new FormattedText(Maximum.ToString("F0"), CultureInfo.CurrentCulture,
                                                               FlowDirection.LeftToRight, typeFace, FontSize, Fill,
                                                               VisualTreeHelper.GetDpi(this).PixelsPerDip);

            MinHeight = Math.Max(MinHeight, textSizingSample.Height);
        }
    }
}
