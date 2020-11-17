using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VideoScriptEditor.ViewModels.Common;

namespace VideoScriptEditor.Views.Common
{
    /// <summary>
    /// Represents a control that can be used to display or edit the
    /// location and size properties of a data bound <see cref="Rect"/> structure.
    /// </summary>
    public partial class RectEditBox : UserControl
    {
        /// <inheritdoc cref="RectEditViewModel"/>
        private RectEditViewModel RectViewModel { get; }

        /// <summary>
        /// Identifies the <see cref="BoundRect" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="BoundRect" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty BoundRectProperty = DependencyProperty.Register(
            nameof(BoundRect),
            typeof(Rect),
            typeof(RectEditBox),
            new FrameworkPropertyMetadata(OnBoundRectPropertyChanged));

        /// <summary>
        /// The data bound <see cref="Rect"/> structure.
        /// </summary>
        public Rect BoundRect
        {
            get => (Rect)GetValue(BoundRectProperty);
            set => SetCurrentValue(BoundRectProperty, value);
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="BoundRect"/> property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnBoundRectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RectEditBox)d).RectViewModel.Rect = (Rect)e.NewValue;
        }

        /// <summary>
        /// Creates a new <see cref="RectEditBox"/> element.
        /// </summary>
        public RectEditBox()
        {
            InitializeComponent();

            RectViewModel = new RectEditViewModel();

            _rectLeftTextBox.SetBinding(TextBox.TextProperty, CreateRectViewModelBinding(nameof(RectEditViewModel.Left)));
            _rectTopTextBox.SetBinding(TextBox.TextProperty, CreateRectViewModelBinding(nameof(RectEditViewModel.Top)));
            _rectWidthTextBox.SetBinding(TextBox.TextProperty, CreateRectViewModelBinding(nameof(RectEditViewModel.Width)));
            _rectHeightTextBox.SetBinding(TextBox.TextProperty, CreateRectViewModelBinding(nameof(RectEditViewModel.Height)));

            PropertyChangedEventManager.AddHandler(RectViewModel, OnRectViewModelPropertyChanged, string.Empty);
        }

        /// <summary>
        /// Invoked whenever a <see cref="RectViewModel"/> property changes.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedEventHandler"/>
        private void OnRectViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            BoundRect = RectViewModel.Rect;
        }

        /// <summary>
        /// Creates a two-way <see cref="Binding"/> with an initial path and explicitly sets
        /// the <see cref="RectViewModel"/> as its binding source.
        /// </summary>
        /// <param name="bindingPath">The initial <see cref="Binding.Path"/> for the binding.</param>
        /// <returns>
        /// A new two-way <see cref="Binding"/> with the initial <paramref name="bindingPath"/>
        /// and the <see cref="RectViewModel"/> as its binding source.
        /// </returns>
        private Binding CreateRectViewModelBinding(string bindingPath) => new Binding(bindingPath)
        {
            Source = RectViewModel,
            Mode = BindingMode.TwoWay
        };
    }
}
