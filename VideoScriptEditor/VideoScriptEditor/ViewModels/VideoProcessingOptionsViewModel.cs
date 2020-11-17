using Prism.Mvvm;
using System;
using VideoScriptEditor.Models;
using Ratio = VideoScriptEditor.Models.Primitives.Ratio;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.ViewModels
{
    /// <summary>
    /// View Model for coordinating interaction between a view and a <see cref="VideoProcessingOptionsModel">video processing options</see> model.
    /// </summary>
    public class VideoProcessingOptionsViewModel : BindableBase
    {
        /// <inheritdoc cref="VideoProcessingOptionsModel"/>
        private VideoProcessingOptionsModel Model { get; }

        /// <inheritdoc cref="VideoProcessingOptionsModel.OutputVideoResizeMode"/>
        public VideoResizeMode OutputVideoResizeMode
        {
            get => Model.OutputVideoResizeMode;
            set
            {
                if (Model.OutputVideoResizeMode != value)
                {
                    Model.OutputVideoResizeMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <inheritdoc cref="VideoProcessingOptionsModel.OutputVideoSize"/>
        public SizeI? OutputVideoSize
        {
            get => Model.OutputVideoSize;
            set
            {
                if (Model.OutputVideoSize != value)
                {
                    Model.OutputVideoSize = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <inheritdoc cref="VideoProcessingOptionsModel.OutputVideoAspectRatio"/>
        public Ratio? OutputVideoAspectRatio
        {
            get => Model.OutputVideoAspectRatio;
            set
            {
                if (Model.OutputVideoAspectRatio != value)
                {
                    Model.OutputVideoAspectRatio = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="VideoProcessingOptionsViewModel"/> instance.
        /// </summary>
        /// <param name="model">The <see cref="VideoProcessingOptionsModel">video processing options</see> model providing data for consumption by a view.</param>
        public VideoProcessingOptionsViewModel(VideoProcessingOptionsModel model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }
    }
}
