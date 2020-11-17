using System;
using System.Globalization;
using System.Windows.Data;
using SizeD = System.Windows.Size;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.Views.Common
{
    /// <summary>
    /// Converts a <see cref="System.Windows.Size"/> or <see cref="System.Drawing.Size"/> value
    /// into a <see cref="string"/> in the form of {Width}x{Height}.
    /// </summary>
    public class SizeToWidthTimesHeightStringConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SizeD sizeD)
            {
                if (!sizeD.IsEmpty)
                {
                    return $"{sizeD.Width}x{sizeD.Height}";
                }
            }
            else if (value is SizeI sizeI)
            {
                if (!sizeI.IsEmpty)
                {
                    return $"{sizeI.Width}x{sizeI.Height}";
                }
            }

            // Use FallbackValue
            return System.Windows.DependencyProperty.UnsetValue;
        }

        /// <summary>Not supported. Don't call this method.</summary>
        /// <returns>Throws a <see cref="NotSupportedException"/></returns>
        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
