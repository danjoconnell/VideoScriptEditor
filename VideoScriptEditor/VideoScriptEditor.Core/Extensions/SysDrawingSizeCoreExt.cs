using Size = System.Drawing.Size;
using Ratio = VideoScriptEditor.Models.Primitives.Ratio;

namespace VideoScriptEditor.Extensions
{
    /// <summary>
    /// Core extension methods for the <see cref="System.Drawing.Size"/> structure.
    /// </summary>
    public static class SysDrawingSizeCoreExt
    {
        /// <summary>
        /// Expands the <see cref="Size"/> to the nearest even integral dimensions that satisfy the specified aspect ratio.
        /// </summary>
        /// <param name="size">The source <see cref="Size"/> structure.</param>
        /// <param name="aspectRatio">The target <see cref="Ratio">aspect ratio</see> for expansion.</param>
        /// <returns>An expanded <see cref="Size"/> with even integral dimensions that satisfy the specified aspect ratio.</returns>
        public static Size ExpandToAspectRatio(this Size size, Ratio aspectRatio)
        {
            double outputWidth, outputHeight;

            // resize using original width
            outputWidth = size.Width;
            outputHeight = outputWidth * ((double)aspectRatio.Denominator / aspectRatio.Numerator);

            if (outputHeight < size.Height)
            {
                // resize using original height
                outputHeight = size.Height;
                outputWidth = outputHeight * ((double)aspectRatio.Numerator / aspectRatio.Denominator);
            }

            return new Size(
                outputWidth.RoundToNearestEvenIntegral(),
                outputHeight.RoundToNearestEvenIntegral()
            );
        }
    }
}
