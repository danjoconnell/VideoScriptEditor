using System.ComponentModel;
using System.Windows;

namespace VideoScriptEditor.ViewModels.Common
{
    /// <summary>
    /// View Model encapsulating presentation logic for a view that provides controls
    /// for editing a <see cref="System.Windows.Rect"/> structure.
    /// </summary>
    public class RectEditViewModel : INotifyPropertyChanged
    {
        private Rect _rect;

        /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the <see cref="System.Windows.Rect"/> being edited.
        /// </summary>
        public Rect Rect
        {
            get => _rect;
            set
            {
                if (_rect != value)
                {
                    _rect = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <inheritdoc cref="Rect.X"/>
        public double Left
        {
            get => _rect.X;
            set
            {
                if (_rect.X != value)
                {
                    _rect.X = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <inheritdoc cref="Rect.Y"/>
        public double Top
        {
            get => _rect.Y;
            set
            {
                if (_rect.Y != value)
                {
                    _rect.Y = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <inheritdoc cref="Rect.Width"/>
        public double Width
        {
            get => _rect.Width;
            set
            {
                if (_rect.Width != value)
                {
                    _rect.Width = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <inheritdoc cref="Rect.Height"/>
        public double Height
        {
            get => _rect.Height;
            set
            {
                if (_rect.Height != value)
                {
                    _rect.Height = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="RectEditViewModel"/> instance.
        /// </summary>
        public RectEditViewModel()
        {
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for all properties.
        /// </summary>
        private void OnPropertyChanged()
        {
            // A null or empty PropertyChangedEventArgs property name string indicates all properties have changed.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
