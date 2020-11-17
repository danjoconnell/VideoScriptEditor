using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using VideoScriptEditor.Models.Primitives;

namespace VideoScriptEditor.Models.Masking.Shapes
{
    /// <summary>
    /// Model encapsulating polygon masking shape segment key frame data.
    /// </summary>
    [Serializable()]
    [DataContract(Name = "Polygon", Namespace = "")]
    public class PolygonMaskShapeKeyFrameModel : MaskShapeKeyFrameModelBase, IEquatable<PolygonMaskShapeKeyFrameModel>
    {
        /// <summary>
        /// A collection of points that make up the polygon.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public List<PointD> Points { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="PolygonMaskShapeKeyFrameModel"/> class.
        /// </summary>
        /// <param name="frameNumber">The zero-based frame number of the key frame. Defaults to zero.</param>
        /// <param name="points">
        /// A collection of <see cref="PointD">points</see> that make up the polygon.
        /// If not specified, an empty collection is created.
        /// </param>
        public PolygonMaskShapeKeyFrameModel(int frameNumber = 0, List<PointD> points = null) : base(frameNumber)
        {
            Points = points ?? new List<PointD>();
        }

        /// <inheritdoc/>
        public override KeyFrameModelBase DeepCopy()
        {
            return new PolygonMaskShapeKeyFrameModel(FrameNumber, new List<PointD>(Points));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as PolygonMaskShapeKeyFrameModel);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] PolygonMaskShapeKeyFrameModel other)
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
            return Points == other.Points && base.Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Points);
        }
    }
}
