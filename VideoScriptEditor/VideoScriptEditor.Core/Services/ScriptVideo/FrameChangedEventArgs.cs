using System;

namespace VideoScriptEditor.Services.ScriptVideo
{
    /// <summary>
    /// Provides data for the <see cref="IScriptVideoService.FrameChanged"/> event.
    /// </summary>
    public class FrameChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The zero-based video frame number prior to the frame being changed.
        /// </summary>
        public int PreviousFrameNumber { get; }

        /// <summary>
        /// The current zero-based video frame number.
        /// </summary>
        public int CurrentFrameNumber { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="FrameChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousFrameNumber">The zero-based video frame number prior to the frame being changed.</param>
        /// <param name="currentFrameNumber">The current zero-based video frame number.</param>
        public FrameChangedEventArgs(int previousFrameNumber, int currentFrameNumber)
        {
            PreviousFrameNumber = previousFrameNumber;
            CurrentFrameNumber = currentFrameNumber;
        }
    }
}
