using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using VideoScriptEditor.Converters;

namespace VideoScriptEditor.Models
{
    /// <summary>
    /// Specifies the method for performing video resize.
    /// </summary>
    [Serializable()]
    [DataContract(Namespace = "")]
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum VideoResizeMode
    {
        /// <summary>
        /// No resize, the original video width and height are retained.
        /// </summary>
        [EnumMember]
        None,

        /// <summary>
        /// Letterbox to size.
        /// </summary>
        [EnumMember]
        [Description("Letterbox to size")]
        LetterboxToSize,

        /// <summary>
        /// Letterbox to aspect ratio.
        /// </summary>
        [EnumMember]
        [Description("Letterbox to aspect ratio")]
        LetterboxToAspectRatio
    }
}
