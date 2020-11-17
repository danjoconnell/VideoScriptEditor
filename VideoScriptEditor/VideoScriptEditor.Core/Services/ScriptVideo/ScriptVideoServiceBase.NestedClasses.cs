using VideoScriptEditor.Models;

namespace VideoScriptEditor.Services.ScriptVideo
{
    public abstract partial class ScriptVideoServiceBase
    {
        /// <summary>
        /// Specifies the reason for canceling the video playback operation in the service.
        /// </summary>
        protected enum PlayVideoTaskCancellationReason
        {
            /// <summary>
            /// A generic cancel request.
            /// </summary>
            None,

            /// <summary>
            /// Pause video playback requested.
            /// </summary>
            PauseRequested,

            /// <summary>
            /// Stop video playback requested.
            /// </summary>
            StopRequested,

            /// <summary>
            /// The loaded script is being closed.
            /// </summary>
            CloseScriptRequested
        }


        /// <summary>
        /// Nested class providing data for notifying the service
        /// that the video playback operation should be canceled.
        /// </summary>
        protected class PlayVideoTaskCancellationContext
        {
            private readonly object _syncLock = new object();
            private bool _isCancellationRequested;
            private PlayVideoTaskCancellationReason _cancellationReason;

            /// <summary>
            /// Gets or sets whether cancellation of the video playback operation is requested.
            /// </summary>
            public bool IsCancellationRequested
            {
                get => _isCancellationRequested;
                set
                {
                    lock (_syncLock)
                    {
                        _isCancellationRequested = value;
                    }
                }
            }

            /// <summary>
            /// Gets or sets the reason for canceling the video playback operation.
            /// </summary>
            public PlayVideoTaskCancellationReason CancellationReason
            {
                get => _cancellationReason;
                set
                {
                    lock (_syncLock)
                    {
                        _cancellationReason = value;
                    }
                }
            }

            /// <summary>
            /// Creates a new instance of the <see cref="PlayVideoTaskCancellationContext"/> class.
            /// </summary>
            public PlayVideoTaskCancellationContext()
            {
                _isCancellationRequested = false;
                _cancellationReason = PlayVideoTaskCancellationReason.None;
            }

            /// <summary>
            /// Resets context values.
            /// </summary>
            public void Reset()
            {
                lock (_syncLock)
                {
                    _isCancellationRequested = false;
                    _cancellationReason = PlayVideoTaskCancellationReason.None;
                }
            }

            /// <summary>
            /// Sets context values for a Pause video playback request.
            /// </summary>
            public void SetPause()
            {
                lock (_syncLock)
                {
                    _cancellationReason = PlayVideoTaskCancellationReason.PauseRequested;
                    _isCancellationRequested = true;
                }
            }

            /// <summary>
            /// Sets context values for a Stop video playback request.
            /// </summary>
            public void SetStop()
            {
                lock (_syncLock)
                {
                    _cancellationReason = PlayVideoTaskCancellationReason.StopRequested;
                    _isCancellationRequested = true;
                }
            }

            /// <summary>
            /// Sets context values for requesting video playback cancellation
            /// due to the loaded script being closed in the service.
            /// </summary>
            public void SetCloseScript()
            {
                lock (_syncLock)
                {
                    _cancellationReason = PlayVideoTaskCancellationReason.CloseScriptRequested;
                    _isCancellationRequested = true;
                }
            }
        }

        /// <summary>
        /// Nested structure containing data for performing segment model key frame linear interpolation.
        /// </summary>
        protected struct SegmentKeyFrameLerpDataItem
        {
            /// <summary><see cref="SegmentModelBase.TrackNumber">Segment Model Track Number</see></summary>
            public int TrackNumber;

            /// <summary>The <see cref="KeyFrameModelBase">key frame</see> at or before the interpolation target frame number</summary>
            public KeyFrameModelBase KeyFrameAtOrBefore;

            /// <summary>The <see cref="KeyFrameModelBase">key frame</see> after the interpolation target frame number</summary>
            /// <remarks>May be null if no interpolation is required.</remarks>
            public KeyFrameModelBase KeyFrameAfter;

            /// <summary>A value between 0 and 1 that indicates the weight of <see cref="KeyFrameAfter"/>.</summary>
            public double LerpAmount;

            /// <summary>
            /// Creates a new instance of the <see cref="SegmentKeyFrameLerpDataItem"/> nested structure.
            /// </summary>
            /// <param name="trackNumber">The value to set for field <see cref="TrackNumber"/>.</param>
            /// <param name="keyFrameAtOrBefore">The value to set for field <see cref="KeyFrameAtOrBefore"/>.</param>
            /// <param name="keyFrameAfter">The value to set for field <see cref="KeyFrameAfter"/>.</param>
            /// <param name="lerpAmount">The value to set for field <see cref="LerpAmount"/>.</param>
            public SegmentKeyFrameLerpDataItem(int trackNumber, KeyFrameModelBase keyFrameAtOrBefore, KeyFrameModelBase keyFrameAfter, double lerpAmount)
            {
                TrackNumber = trackNumber;
                KeyFrameAtOrBefore = keyFrameAtOrBefore;
                KeyFrameAfter = keyFrameAfter;
                LerpAmount = lerpAmount;
            }
        }
    }
}
