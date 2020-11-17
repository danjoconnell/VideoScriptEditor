using System;
using System.Diagnostics.CodeAnalysis;

namespace VideoScriptEditor.Models.Masking
{
    /// <summary>
    /// Base class for masking shape segment key frame models.
    /// </summary>
    [Serializable()]
    public abstract class MaskShapeKeyFrameModelBase : KeyFrameModelBase, IEquatable<MaskShapeKeyFrameModelBase>
    {
        /// <summary>
        /// Base constructor for classes derived from the <see cref="MaskShapeKeyFrameModelBase"/> class.
        /// </summary>
        /// <param name="frameNumber">The zero-based frame number of the key frame. Defaults to zero.</param>
        protected MaskShapeKeyFrameModelBase(int frameNumber = 0) : base(frameNumber)
        {
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as MaskShapeKeyFrameModelBase);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] MaskShapeKeyFrameModelBase other)
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
