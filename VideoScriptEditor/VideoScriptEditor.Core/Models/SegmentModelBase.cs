using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using VideoScriptEditor.Collections;

namespace VideoScriptEditor.Models
{
    /// <summary>
    /// Base class for Segment Models.
    /// </summary>
    [Serializable()]
    [DataContract(Name = "Segment", Namespace = "")]
    public abstract class SegmentModelBase : IEquatable<SegmentModelBase>, IComparable<SegmentModelBase>
    {
        /// <summary>
        /// A sorted collection of key frames in this segment.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public KeyFrameModelCollection KeyFrames { get; protected set; }

        /// <summary>
        /// The inclusive zero-based start frame number of this segment.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public int StartFrame { get; set; }

        /// <summary>
        /// The inclusive zero-based end frame number of this segment.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public int EndFrame { get; set; }

        /// <summary>
        /// The zero-based timeline track number of this segment.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public int TrackNumber { get; set; }

        /// <summary>
        /// A descriptive name for this segment.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Base constructor for classes derived from the <see cref="SegmentModelBase"/> class.
        /// </summary>
        /// <param name="startFrame">The inclusive zero-based start frame number of the segment.</param>
        /// <param name="endFrame">The inclusive zero-based end frame number of the segment.</param>
        /// <param name="trackNumber">The zero-based timeline track number of the segment. Defaults to zero.</param>
        /// <param name="keyFrames">A sorted collection of key frames in the segment. Defaults to a null <see cref="KeyFrameModelCollection"/>.</param>
        /// <param name="name">A descriptive name for the segment. Defaults to a null <see cref="string"/>.</param>
        protected SegmentModelBase(int startFrame, int endFrame, int trackNumber = 0, KeyFrameModelCollection keyFrames = null, string name = null)
        {
            StartFrame = startFrame;
            EndFrame = endFrame;
            TrackNumber = trackNumber;
            KeyFrames = keyFrames ?? new KeyFrameModelCollection();
            Name = name;
        }

        /// <summary>
        /// Determines whether the specified frame number is within the segment's inclusive
        /// start and end frame range.
        /// </summary>
        /// <param name="frameNumber">The frame number to check.</param>
        /// <returns><see langword="true"/> if the specified frame number is within the segment's inclusive frame range; otherwise, <see langword="false"/>.</returns>
        public bool IsFrameWithin(int frameNumber)
        {
            return frameNumber >= StartFrame && frameNumber <= EndFrame;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as SegmentModelBase);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] SegmentModelBase other)
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
            return StartFrame == other.StartFrame
                   && EndFrame == other.EndFrame
                   && TrackNumber == other.TrackNumber
                   && Name == other.Name
                   && KeyFrames == other.KeyFrames;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(KeyFrames, StartFrame, EndFrame, TrackNumber, Name);
        }

        /// <inheritdoc/>
        public virtual int CompareTo(SegmentModelBase other)
        {
            // If other is not a valid object reference, this instance is greater. 
            if (other == null) return 1;

            if (TrackNumber != other.TrackNumber)
            {
                return TrackNumber.CompareTo(other.TrackNumber);
            }

            // Same track
            if (other.StartFrame < StartFrame || other.StartFrame > EndFrame)
            {
                // Not overlapping
                return StartFrame.CompareTo(other.StartFrame);
            }

            // Equal or overlapping
            return 0;
        }
    }
}
