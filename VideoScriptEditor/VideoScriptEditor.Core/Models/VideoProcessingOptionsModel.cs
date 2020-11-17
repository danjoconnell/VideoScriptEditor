using System;
using System.Runtime.Serialization;
using VideoScriptEditor.Models.Primitives;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.Models
{
    /// <summary>
    /// Model encapsulating video processing options such as video resizing.
    /// </summary>
    [Serializable()]
    [DataContract(Name = "VideoProcessingOptions", Namespace = "")]
    public class VideoProcessingOptionsModel
    {
        /// <summary>
        /// Specifies the video resize method.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public VideoResizeMode OutputVideoResizeMode { get; set; }

        /// <summary>
        /// The desired size of the video in pixels.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public SizeI? OutputVideoSize { get; set; }

        /// <summary>
        /// The desired aspect ratio if the video is to be resized using an aspect ratio.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public Ratio? OutputVideoAspectRatio { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="VideoProcessingOptionsModel"/> class.
        /// </summary>
        public VideoProcessingOptionsModel()
        {
            OutputVideoResizeMode = VideoResizeMode.None;
            OutputVideoSize = null;
            OutputVideoAspectRatio = null;
        }
    }
}
