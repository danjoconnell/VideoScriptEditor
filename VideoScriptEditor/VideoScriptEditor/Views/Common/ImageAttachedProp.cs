using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VideoScriptEditor.Views.Common
{
    /// <summary>
    /// Attached properties for <see cref="Image"/> controls.
    /// </summary>
    public class ImageAttachedProp
    {
        /// <summary>
        /// Identifies the <see cref="ImageAttachedProp.SourceResourceKey"/> attached property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="ImageAttachedProp.SourceResourceKey"/> attached property.
        /// </returns>
        public static readonly DependencyProperty SourceResourceKeyProperty =
            DependencyProperty.RegisterAttached("SourceResourceKey", typeof(string), typeof(ImageAttachedProp),
                new PropertyMetadata("", new PropertyChangedCallback(OnSourceResourceKeyPropertyChanged)));


        /// <summary>
        /// Gets the value of the <see cref="ImageAttachedProp.SourceResourceKey"/> attached property
        /// for a given <see cref="Image"/> control.
        /// </summary>
        /// <param name="image">The <see cref="Image"/> control from which the property value is read.</param>
        /// <returns>The resource key of the <see cref="Image.Source"/>.</returns>
        [AttachedPropertyBrowsableForType(typeof(Image))]
        public static string GetSourceResourceKey(Image image) => (string)image.GetValue(SourceResourceKeyProperty);

        /// <summary>
        /// Sets the value of the <see cref="ImageAttachedProp.SourceResourceKey"/> attached property
        /// for a given <see cref="Image"/> control.
        /// </summary>
        /// <param name="image">The <see cref="Image"/> control to which the property value is written.</param>
        /// <param name="value">Sets the resource key of the <see cref="Image.Source"/>.</param>
        public static void SetSourceResourceKey(Image image, string value)
        {
            image.SetValue(SourceResourceKeyProperty, value);
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="ImageAttachedProp.SourceResourceKey"/> attached property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnSourceResourceKeyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Image imageElement = (Image)d;
            string resourceKey = e.NewValue as string;

            if (!string.IsNullOrWhiteSpace(resourceKey) && imageElement.TryFindResource(resourceKey) is ImageSource imageSource)
            {
                imageElement.Source = imageSource;
            }
            else
            {
                imageElement.Source = null; // default value.
            }
        }
    }
}
