namespace VideoScriptEditor.Models
{
    /// <summary>
    /// Extension methods for the <see cref="Primitives.PointD"/> structure.
    /// </summary>
    public static class PointDExtensions
    {

        /// <summary>
        /// Converts a <see cref="Primitives.PointD"/> structure to an equivalent <see cref="System.Windows.Point">WPF Point</see> structure.
        /// </summary>
        /// <param name="pointD">The source <see cref="Primitives.PointD"/> structure.</param>
        /// <returns>A <see cref="System.Windows.Point">WPF Point</see> structure equivalent to the <see cref="Primitives.PointD"/> structure.</returns>
        public static System.Windows.Point ToWpfPoint(this Primitives.PointD pointD)
        {
            return new System.Windows.Point(pointD.X, pointD.Y);
        }

        /// <summary>
        /// Converts a <see cref="System.Windows.Point">WPF Point</see> structure to an equivalent <see cref="Primitives.PointD"/> structure.
        /// </summary>
        /// <param name="wpfPoint">The source <see cref="System.Windows.Point">WPF Point</see> structure.</param>
        /// <returns>A <see cref="Primitives.PointD"/> structure equivalent to the <see cref="System.Windows.Point">WPF Point</see> structure.</returns>
        public static Primitives.PointD ToPointD(this System.Windows.Point wpfPoint)
        {
            return new Primitives.PointD(wpfPoint.X, wpfPoint.Y);
        }

        /// <summary>
        /// Determines whether a given <see cref="Primitives.PointD"/> structure is equivalently equal to a given <see cref="System.Windows.Point">WPF Point</see> structure.
        /// </summary>
        /// <param name="pointD">The <see cref="Primitives.PointD"/> structure to compare.</param>
        /// <param name="wpfPoint">The <see cref="System.Windows.Point">WPF Point</see> structure to compare.</param>
        /// <returns><c>True</c> if the <see cref="Primitives.PointD"/> structure is equivalently equal to the <see cref="System.Windows.Point">WPF Point</see> structure, <c>False</c> otherwise.</returns>
        public static bool IsEqualTo(this Primitives.PointD pointD, System.Windows.Point wpfPoint)
        {
            return PointsAreEqual(pointD, wpfPoint);
        }

        /// <summary>
        /// Determines whether a given <see cref="System.Windows.Point">WPF Point</see> structure is equivalently equal to a given <see cref="Primitives.PointD"/> structure.
        /// </summary>
        /// <param name="wpfPoint">The <see cref="System.Windows.Point">WPF Point</see> structure to compare.</param>
        /// <param name="pointD">The <see cref="Primitives.PointD"/> structure to compare.</param>
        /// <returns><c>True</c> if the <see cref="System.Windows.Point">WPF Point</see> structure is equivalently equal to the <see cref="Primitives.PointD"/> structure, <c>False</c> otherwise.</returns>
        public static bool IsEqualTo(this System.Windows.Point wpfPoint, Primitives.PointD pointD)
        {
            return PointsAreEqual(pointD, wpfPoint);
        }

        /// <summary>
        /// Determines whether a given <see cref="Primitives.PointD"/> structure and a given <see cref="System.Windows.Point">WPF Point</see> structure are equivalently equal.
        /// </summary>
        /// <param name="pointD">The <see cref="Primitives.PointD"/> structure to compare.</param>
        /// <param name="wpfPoint">The <see cref="System.Windows.Point">WPF Point</see> structure to compare.</param>
        /// <returns><c>True</c> if the <see cref="Primitives.PointD"/> structure and the <see cref="System.Windows.Point">WPF Point</see> structure are equivalently equal, <c>False</c> otherwise.</returns>
        private static bool PointsAreEqual(Primitives.PointD pointD, System.Windows.Point wpfPoint)
        {
            return pointD.X == wpfPoint.X && pointD.Y == wpfPoint.Y;
        }
    }
}
