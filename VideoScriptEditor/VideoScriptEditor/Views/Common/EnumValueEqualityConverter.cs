using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VideoScriptEditor.Views.Common
{
    /// <summary>
    /// Converter for determining if an <see cref="Enum"/> value and
    /// an <see cref="Enum"/> value passed as a parameter are equal.
    /// </summary>
    public class EnumValueEqualityConverter : IValueConverter
    {
        /// <summary>
        /// Determines if the <paramref name="value"/> and <paramref name="parameter"/>
        /// <see cref="Enum"/> values are equal.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> is an enumeration value of the same type and with the same underlying
        /// value as <paramref name="parameter"/>; otherwise, <c>false</c>;
        /// or <see cref="DependencyProperty.UnsetValue"/> if <paramref name="value"/> is not an enumeration value.
        /// </returns>
        /// <inheritdoc cref="IValueConverter.Convert(object, Type, object, CultureInfo)"/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum enumValue)
            {
                return enumValue.Equals(parameter);
            }

            return DependencyProperty.UnsetValue;
        }

        /// <inheritdoc cref="IValueConverter.ConvertBack(object, Type, object, CultureInfo)"/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.Equals(true) /* value is Boolean type and equals True */
                && parameter is Enum enumValue)
            {
                return enumValue;
            }

            return Binding.DoNothing;
        }
    }
}
