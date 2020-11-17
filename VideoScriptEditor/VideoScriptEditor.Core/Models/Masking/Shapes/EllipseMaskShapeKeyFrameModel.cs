using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using VideoScriptEditor.Models.Primitives;

namespace VideoScriptEditor.Models.Masking.Shapes
{
    /// <summary>
    /// Model encapsulating ellipse masking shape segment key frame data.
    /// </summary>
    [Serializable()]
    [DataContract(Name = "Ellipse", Namespace = "")]
    public class EllipseMaskShapeKeyFrameModel : MaskShapeKeyFrameModelBase, IEquatable<EllipseMaskShapeKeyFrameModel>
    {
        /// <summary>
        /// The center point of the ellipse.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public PointD CenterPoint { get; set; }

        /// <summary>
        /// The x-radius value of the ellipse.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public double RadiusX { get; set; }

        /// <summary>
        /// The y-radius value of the ellipse.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public double RadiusY { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="EllipseMaskShapeKeyFrameModel"/> class.
        /// </summary>
        /// <param name="frameNumber">The zero-based frame number of the key frame. Defaults to zero.</param>
        /// <param name="centerPoint">The center point of the ellipse. Defaults to the top-left corner (0,0).</param>
        /// <param name="radiusX">The x-radius value of the ellipse. Defaults to zero.</param>
        /// <param name="radiusY">The y-radius value of the ellipse. Defaults to zero.</param>
        public EllipseMaskShapeKeyFrameModel(int frameNumber = 0, PointD centerPoint = default, double radiusX = 0d, double radiusY = 0d) : base(frameNumber)
        {
            CenterPoint = centerPoint;
            RadiusX = radiusX;
            RadiusY = radiusY;
        }

        /// <inheritdoc/>
        public override KeyFrameModelBase DeepCopy()
        {
            return new EllipseMaskShapeKeyFrameModel(FrameNumber, CenterPoint, RadiusX, RadiusY);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as EllipseMaskShapeKeyFrameModel);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] EllipseMaskShapeKeyFrameModel other)
        {
            // If parameter is null, return false.
            if (other is null)
            {
                return false;
            }

            // Optimization for a common success case.
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // Check properties that this class declares
            // and let base class check its own fields and do the run-time type comparison.
            return CenterPoint == other.CenterPoint
                   && RadiusX == other.RadiusX && RadiusY == other.RadiusY
                   && base.Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), CenterPoint, RadiusX, RadiusY);
        }
    }
}
