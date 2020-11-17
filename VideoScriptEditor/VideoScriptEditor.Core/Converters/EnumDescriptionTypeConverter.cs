/*
    Based on sample code written by Brian Lagunas on his blog at https://brianlagunas.com/a-better-way-to-data-bind-enums-in-wpf/
    and in his 'BindingEnumsInWpf' GitHub repository at https://github.com/brianlagunas/BindingEnumsInWpf/blob/master/BindingEnums/EnumDescriptionTypeConverter.cs
*/
using System;
using System.ComponentModel;
using System.Reflection;

namespace VideoScriptEditor.Converters
{
    /// <summary>
    /// Provides a type converter to convert a <see cref="DescriptionAttribute"/> on an enum value into a string.
    /// </summary>
    public class EnumDescriptionTypeConverter : EnumConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumDescriptionTypeConverter"/> class for the given type.
        /// </summary>
        public EnumDescriptionTypeConverter(Type type)
            : base(type)
        {
        }

        /// <inheritdoc/>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value != null)
                {
                    FieldInfo fi = value.GetType().GetField(value.ToString());
                    if (fi != null)
                    {
                        var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
                        return ((attributes.Length > 0) && (!string.IsNullOrEmpty(attributes[0].Description))) ? attributes[0].Description : value.ToString();
                    }
                }

                return string.Empty;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
