using System.ComponentModel;
using System.Windows;

namespace VideoScriptEditor.ViewModels.Common
{
    /// <summary>
    /// View Model encapsulating presentation logic for a view that provides controls
    /// for editing a <see cref="System.Windows.Point"/> structure.
    /// </summary>
    public class PointEditViewModel : INotifyPropertyChanged
    {
        private Point _point;

        /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the <see cref="System.Windows.Point"/> being edited.
        /// </summary>
        public Point Point
        {
            get => _point;
            set
            {
                if (_point != value)
                {
                    _point = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <inheritdoc cref="Point.X"/>
        public double X
        {
            get => _point.X;
            set
            {
                if (_point.X != value)
                {
                    _point.X = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <inheritdoc cref="Point.Y"/>
        public double Y
        {
            get => _point.Y;
            set
            {
                if (_point.Y != value)
                {
                    _point.Y = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="PointEditViewModel"/> instance.
        /// </summary>
        public PointEditViewModel()
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
