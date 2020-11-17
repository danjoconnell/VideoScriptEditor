using System.Windows;
using VideoScriptEditor.Models.Masking.Shapes;

namespace VideoScriptEditor.Models
{
    /// <summary>
    /// Extension methods for masking data models.
    /// </summary>
    public static class MaskingModelExtensions
    {
        /// <summary>
        /// Creates a <see cref="Rect"/> structure from the
        /// <see cref="RectangleMaskShapeKeyFrameModel.Left">Left</see>, <see cref="RectangleMaskShapeKeyFrameModel.Top">Top</see>,
        /// <see cref="RectangleMaskShapeKeyFrameModel.Width">Width</see> and <see cref="RectangleMaskShapeKeyFrameModel.Height">Height</see>
        /// property values of a <see cref="RectangleMaskShapeKeyFrameModel">rectangle masking key frame model</see>.
        /// </summary>
        /// <param name="rectangleMaskShapeKeyFrameModel">The source <see cref="RectangleMaskShapeKeyFrameModel">rectangle masking key frame model</see>.</param>
        /// <returns>A <see cref="Rect"/> structure.</returns>
        public static Rect ToRect(this RectangleMaskShapeKeyFrameModel rectangleMaskShapeKeyFrameModel)
        {
            return new Rect(rectangleMaskShapeKeyFrameModel.Left, rectangleMaskShapeKeyFrameModel.Top, rectangleMaskShapeKeyFrameModel.Width, rectangleMaskShapeKeyFrameModel.Height);
        }
    }
}
