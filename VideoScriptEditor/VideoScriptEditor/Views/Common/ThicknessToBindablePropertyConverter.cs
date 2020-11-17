using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VideoScriptEditor.Views.Common
{
    /// <summary>
    /// Converter for getting an individual <see cref="Thickness"/> property value for data binding.
    /// </summary>
    public class ThicknessToBindablePropertyConverter : IValueConverter
    {
        /// <summary>
        /// Gets an individual <see cref="Thickness"/> property value for data binding.
        /// </summary>
        /// <remarks>
        /// The individual <see cref="Thickness"/> property value to get is specified by
        /// providing the name of the property in the <paramref name="parameter"/>.
        /// </remarks>
        /// <inheritdoc cref="IValueConverter.Convert(object, Type, object, CultureInfo)"/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Thickness thickness && parameter is string propertyName)
            {
                switch (propertyName)
                {
                    case nameof(Thickness.Left):
                        return thickness.Left;
                    case nameof(Thickness.Top):
                        return thickness.Top;
                    case nameof(Thickness.Right):
                        return thickness.Right;
                    case nameof(Thickness.Bottom):
                        return thickness.Bottom;
                }
            }

            return Binding.DoNothing;
        }

        /// <summary>Not supported. Don't call this method.</summary>
        /// <returns>Throws a <see cref="NotSupportedException"/></returns>
        /// <inheritdoc cref="IValueConverter.ConvertBack(object, Type, object, CultureInfo)"/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
