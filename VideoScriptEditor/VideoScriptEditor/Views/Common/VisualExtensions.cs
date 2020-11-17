using System.Windows.Media;

namespace VideoScriptEditor.Views.Common
{
    /// <summary>
    /// Provides extension methods and helpers for <see cref="Visual"/> objects.
    /// </summary>
    public static class VisualExtensions
    {
        /// <summary>
        /// Finds the first instance of a strongly typed child <see cref="Visual"/> within the specified parent <see cref="Visual"/>.
        /// </summary>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/desktop/wpf/data/how-to-find-datatemplate-generated-elements.
        /// </remarks>
        /// <typeparam name="T">The type of the child <see cref="Visual"/> to find.</typeparam>
        /// <param name="parent">The parent <see cref="Visual"/>.</param>
        /// <returns>
        /// The strongly typed child <see cref="Visual"/> instance
        /// or <c>null</c> if no strongly typed child <see cref="Visual"/> was found within the <paramref name="parent"/>.
        /// </returns>
        public static T FindVisualChild<T>(this Visual parent) where T : Visual
        {
            T childVisual = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount && childVisual == null; i++)
            {
                if (!(VisualTreeHelper.GetChild(parent, i) is Visual child))
                {
                    break;
                }

                childVisual = child as T;
                if (childVisual == null)
                {
                    childVisual = FindVisualChild<T>(child);
                }
            }

            return childVisual;
        }
    }
}
