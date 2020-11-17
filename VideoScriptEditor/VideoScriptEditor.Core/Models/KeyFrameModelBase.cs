using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace VideoScriptEditor.Models
{
    /// <summary>
    /// Base class for key frame models.
    /// </summary>
    [Serializable()]
    [KnownType(typeof(Cropping.CropKeyFrameModel))]
    [KnownType(typeof(Masking.Shapes.PolygonMaskShapeKeyFrameModel))]
    [KnownType(typeof(Masking.Shapes.RectangleMaskShapeKeyFrameModel))]
    [KnownType(typeof(Masking.Shapes.EllipseMaskShapeKeyFrameModel))]
    [DataContract(Name = "KeyFrame", Namespace = "")]
    public abstract class KeyFrameModelBase : IEquatable<KeyFrameModelBase>, IComparable<KeyFrameModelBase>
    {
        /// <summary>
        /// The zero-based frame number of this key frame.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public int FrameNumber { get; set; }

        /// <summary>
        /// Base constructor for key frame models derived from the <see cref="KeyFrameModelBase"/> class.
        /// </summary>
        /// <param name="frameNumber">The zero-based frame number of the key frame. Defaults to zero.</param>
        protected KeyFrameModelBase(int frameNumber = 0)
        {
            FrameNumber = frameNumber;
        }

        /// <summary>
        /// Creates a deep copy of the current <see cref="KeyFrameModelBase"/>.
        /// </summary>
        /// <returns>A deep copy of the current <see cref="KeyFrameModelBase"/>.</returns>
        public abstract KeyFrameModelBase DeepCopy();

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as KeyFrameModelBase);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] KeyFrameModelBase other)
        {
            if (other is null)
            {
                return false;
            }

            // Optimization for a common success case.
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (GetType() != other.GetType())
            {
                return false;
            }

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return FrameNumber == other.FrameNumber;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(FrameNumber);
        }

        /// <inheritdoc cref="IComparable.CompareTo(object?)"/>
        public virtual int CompareTo(KeyFrameModelBase other)
        {
            // If other is not a valid object reference, this instance is greater. 
            if (other == null) return 1;

            return FrameNumber.CompareTo(other.FrameNumber);
        }
    }
}
