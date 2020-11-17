namespace VideoScriptEditor.Geometry
{
    /// <summary>
    /// Extension methods for the <see cref="System.Drawing.Size"/> structure.
    /// </summary>
    public static class SysDrawingSizeExt
    {
        /// <summary>
        /// Converts a <see cref="System.Drawing.Size"/> structure to an equivalent <see cref="System.Windows.Size">WPF Size</see> structure.
        /// </summary>
        /// <param name="size">The source <see cref="System.Drawing.Size"/> structure.</param>
        /// <returns>A <see cref="System.Windows.Size">WPF Size</see> structure equivalent to the <see cref="System.Drawing.Size"/> structure.</returns>
        public static System.Windows.Size ToWpfSize(this System.Drawing.Size size)
        {
            return new System.Windows.Size(size.Width, size.Height);
        }
    }
}
