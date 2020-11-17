using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.Views.Common
{
    /// <summary>
    /// Converts two bound double-precision width and height values into a <see cref="Rect"/> structure.
    /// </summary>
    public class WidthAndHeightToRectConverter : IMultiValueConverter
    {
        /// <inheritdoc cref="IMultiValueConverter.Convert(object[], Type, object, CultureInfo)"/>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double width && values[1] is double height)
            {
                return new Rect(x: 0d, y: 0d, width, height);
            }

            Debug.Fail("Converter did not produce a value.");
            return DependencyProperty.UnsetValue;
        }

        /// <summary>Not supported. Don't call this method.</summary>
        /// <returns>Throws a <see cref="NotSupportedException"/></returns>
        /// <inheritdoc cref="IMultiValueConverter.ConvertBack(object, Type[], object, CultureInfo)"/>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
