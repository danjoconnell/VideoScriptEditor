using System.Windows;
using VideoScriptEditor.Models.Cropping;

namespace VideoScriptEditor.Models
{
    /// <summary>
    /// Extension methods for cropping data models.
    /// </summary>
    public static class CroppingModelExtensions
    {
        /// <summary>
        /// Creates a <see cref="Rect"/> structure from the
        /// <see cref="CropKeyFrameModel.Left">Left</see>, <see cref="CropKeyFrameModel.Top">Top</see>,
        /// <see cref="CropKeyFrameModel.Width">Width</see> and <see cref="CropKeyFrameModel.Height">Height</see>
        /// property values of a <see cref="CropKeyFrameModel">crop key frame model</see>.
        /// </summary>
        /// <param name="cropKeyFrameModel">The source <see cref="CropKeyFrameModel">crop key frame model</see>.</param>
        /// <returns>A <see cref="Rect"/> structure.</returns>
        public static Rect BaseRect(this CropKeyFrameModel cropKeyFrameModel)
        {
            return new Rect(cropKeyFrameModel.Left, cropKeyFrameModel.Top, cropKeyFrameModel.Width, cropKeyFrameModel.Height);
        }
    }
}
