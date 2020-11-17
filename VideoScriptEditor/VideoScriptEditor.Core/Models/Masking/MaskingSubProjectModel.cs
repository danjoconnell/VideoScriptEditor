using System;
using System.Runtime.Serialization;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Models.Masking.Shapes;

namespace VideoScriptEditor.Models.Masking
{
    /// <summary>
    /// Model encapsulating masking subproject data.
    /// </summary>
    [Serializable()]
    [KnownType(typeof(PolygonMaskShapeModel))]
    [KnownType(typeof(RectangleMaskShapeModel))]
    [KnownType(typeof(EllipseMaskShapeModel))]
    [DataContract(Name = "Masking", Namespace = "")]
    public class MaskingSubProjectModel
    {
        /// <summary>
        /// A collection of <see cref="SegmentModelBase">masking shape segments</see>.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public SegmentModelCollection Shapes { get; private set; }

        /// <summary>
        /// Creates a new instance of the <see cref="MaskingSubProjectModel"/> class.
        /// </summary>
        public MaskingSubProjectModel()
        {
            Shapes = new SegmentModelCollection();
        }
    }
}
