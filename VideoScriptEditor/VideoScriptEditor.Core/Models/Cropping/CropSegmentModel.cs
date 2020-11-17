using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using VideoScriptEditor.Collections;

namespace VideoScriptEditor.Models.Cropping
{
    /// <summary>
    /// Model encapsulating cropping segment data.
    /// </summary>
    [Serializable()]
    [DataContract(Name = "Crop", Namespace = "")]
    public class CropSegmentModel : SegmentModelBase, IEquatable<CropSegmentModel>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CropSegmentModel"/> class.
        /// </summary>
        /// <inheritdoc/>
        public CropSegmentModel(int startFrame, int endFrame, int trackNumber = 0, KeyFrameModelCollection keyFrames = null, string name = null) : base(startFrame, endFrame, trackNumber, keyFrames, name)
        {
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as CropSegmentModel);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] CropSegmentModel other)
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

            // Let base class check its own fields
            // and do the run-time type comparison.
            return base.Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode());
        }
    }
}
