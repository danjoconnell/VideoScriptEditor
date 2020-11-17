using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace VideoScriptEditor.Models.Cropping
{
    /// <summary>
    /// Model encapsulating cropping segment key frame data.
    /// </summary>
    [Serializable()]
    [DataContract(Name = "Crop", Namespace = "")]
    public class CropKeyFrameModel : KeyFrameModelBase, IEquatable<CropKeyFrameModel>
    {
        /// <summary>
        /// The left pixel coordinate of the area to crop.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public double Left { get; set; }

        /// <summary>
        /// The top pixel coordinate of the area to crop.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public double Top { get; set; }

        /// <summary>
        /// The pixel width of the area to crop.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public double Width { get; set; }

        /// <summary>
        /// The pixel height of the area to crop.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public double Height { get; set; }

        /// <summary>
        /// The angle in degrees at which the crop area is rotated.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public double Angle { get; set; }

        /// <summary>
        /// Creates a new <see cref="CropKeyFrameModel"/> instance.
        /// </summary>
        /// <param name="frameNumber">The zero-based frame number of the key frame. Defaults to zero.</param>
        /// <param name="left">The left pixel coordinate of the area to crop. Defaults to zero.</param>
        /// <param name="top">The top pixel coordinate of the area to crop. Defaults to zero.</param>
        /// <param name="width">The pixel width of the area to crop. Defaults to zero.</param>
        /// <param name="height">The pixel height of the area to crop. Defaults to zero.</param>
        /// <param name="angle">The angle in degrees at which the crop area is rotated. Defaults to zero.</param>
        public CropKeyFrameModel(int frameNumber = 0, double left = 0d, double top = 0d, double width = 0d, double height = 0d, double angle = 0d) : base(frameNumber)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
            Angle = angle;
        }

        /// <inheritdoc/>
        public override KeyFrameModelBase DeepCopy()
        {
            return new CropKeyFrameModel(FrameNumber, Left, Top, Width, Height, Angle);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as CropKeyFrameModel);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] CropKeyFrameModel other)
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
            return Left == other.Left && Top == other.Top
                   && Width == other.Width && Height == other.Height
                   && Angle == other.Angle
                   && base.Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Left, Top, Width, Height, Angle);
        }
    }
}
