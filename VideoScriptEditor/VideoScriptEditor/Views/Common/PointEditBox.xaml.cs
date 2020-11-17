using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VideoScriptEditor.ViewModels.Common;

namespace VideoScriptEditor.Views.Common
{
    /// <summary>
    /// Represents a control that can be used to display or edit the
    /// <see cref="Point.X"/> and <see cref="Point.Y"/> property values
    /// of a data bound <see cref="Point"/> structure.
    /// </summary>
    public partial class PointEditBox : UserControl
    {
        /// <inheritdoc cref="PointEditViewModel"/>
        private PointEditViewModel PointViewModel { get; }

        /// <summary>
        /// Identifies the <see cref="BoundPoint" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="BoundPoint" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty BoundPointProperty = DependencyProperty.Register(
            nameof(BoundPoint),
            typeof(Point),
            typeof(PointEditBox),
            new FrameworkPropertyMetadata(OnBoundPointPropertyChanged));

        /// <summary>
        /// The data bound <see cref="Point"/> structure.
        /// </summary>
        public Point BoundPoint
        {
            get => (Point)GetValue(BoundPointProperty);
            set => SetCurrentValue(BoundPointProperty, value);
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="BoundPoint"/> property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnBoundPointPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PointEditBox)d).PointViewModel.Point = (Point)e.NewValue;
        }

        /// <summary>
        /// Creates a new <see cref="PointEditBox"/> element.
        /// </summary>
        public PointEditBox()
        {
            InitializeComponent();

            PointViewModel = new PointEditViewModel();

            _pointXTextBox.SetBinding(TextBox.TextProperty, CreatePointViewModelBinding(nameof(PointEditViewModel.X)));
            _pointYTextBox.SetBinding(TextBox.TextProperty, CreatePointViewModelBinding(nameof(PointEditViewModel.Y)));

            PropertyChangedEventManager.AddHandler(PointViewModel, OnPointViewModelPropertyChanged, string.Empty);
        }

        /// <summary>
        /// Invoked whenever a <see cref="PointViewModel"/> property changes.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedEventHandler"/>
        private void OnPointViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            BoundPoint = PointViewModel.Point;
        }

        /// <summary>
        /// Creates a two-way <see cref="Binding"/> with an initial path and explicitly sets
        /// the <see cref="PointViewModel"/> as its binding source.
        /// </summary>
        /// <param name="bindingPath">The initial <see cref="Binding.Path"/> for the binding.</param>
        /// <returns>
        /// A new two-way <see cref="Binding"/> with the initial <paramref name="bindingPath"/>
        /// and the <see cref="PointViewModel"/> as its binding source.
        /// </returns>
        private Binding CreatePointViewModelBinding(string bindingPath) => new Binding(bindingPath)
        {
            Source = PointViewModel,
            Mode = BindingMode.TwoWay
        };
    }
}
