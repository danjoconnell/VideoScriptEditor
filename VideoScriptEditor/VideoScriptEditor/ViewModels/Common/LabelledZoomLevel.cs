namespace VideoScriptEditor.ViewModels.Common
{
    /// <summary>
    /// Encapsulates a zoom level value and descriptive label.
    /// </summary>
    public class LabelledZoomLevel
    {
        /// <summary>
        /// The zoom level value.
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// The descriptive label for the <see cref="Value">zoom level value</see>.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Creates a new <see cref="LabelledZoomLevel"/> instance.
        /// </summary>
        /// <param name="value">The zoom level value.</param>
        /// <param name="label">The descriptive label for the <paramref name="value"/>.</param>
        public LabelledZoomLevel(double value, string label)
        {
            Value = value;
            Label = label;
        }
    }
}
