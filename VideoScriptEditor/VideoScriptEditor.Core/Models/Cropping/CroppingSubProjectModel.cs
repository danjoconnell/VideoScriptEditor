using System;
using System.Runtime.Serialization;
using VideoScriptEditor.Collections;

namespace VideoScriptEditor.Models.Cropping
{
    /// <summary>
    /// Model encapsulating cropping subproject data.
    /// </summary>
    [Serializable()]
    [KnownType(typeof(CropSegmentModel))]
    [DataContract(Name = "Cropping", Namespace = "")]
    public class CroppingSubProjectModel
    {
        /// <summary>
        /// A collection of <see cref="CropSegmentModel">cropping segments</see>.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public SegmentModelCollection CropSegments { get; private set; }

        /// <summary>
        /// Creates a new instance of the <see cref="CroppingSubProjectModel"/> class.
        /// </summary>
        public CroppingSubProjectModel()
        {
            CropSegments = new SegmentModelCollection();
        }
    }
}
