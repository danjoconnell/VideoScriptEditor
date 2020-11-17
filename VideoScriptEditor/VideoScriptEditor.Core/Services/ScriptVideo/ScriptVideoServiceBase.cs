using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Primitives;
using Size = System.Drawing.Size;

namespace VideoScriptEditor.Services.ScriptVideo
{
    /// <summary>
    /// A base class implementation of the <see cref="IScriptVideoService"/> interface,
    /// providing a service for processing video from an AviSynth script
    /// and previewing the resulting edited video through a Direct2D renderer.
    /// </summary>
    /// <remarks>
    /// Intended only for use as a base class for the ScriptVideoService class in the VideoScriptEditor.PreviewRenderer assembly.
    /// The ScriptVideoService is essentially split up into two parts;
    /// A C# base class containing purely managed code and not practical for coding in C++/CLI
    /// and a C++/CLI derived class leveraging the unmanaged C++ interop handling C++/CLI provides.
    /// </remarks>
    public abstract partial class ScriptVideoServiceBase : IScriptVideoService
    {
        /// <summary>Propagates notification that the video playback operation should be canceled.</summary>
        protected readonly PlayVideoTaskCancellationContext _playVideoTaskCancellationContext;

        /// <summary>
        /// Gets the internal <see cref="ScriptVideoContextBase">runtime context</see>.
        /// </summary>
        protected abstract ScriptVideoContextBase InternalContext { get; }

        /// <summary>
        /// Occurs when an AviSynth script is opened for processing by the service.
        /// </summary>
        public event EventHandler ScriptOpened;

        /// <summary>
        /// Occurs when an AviSynth script that was being processed by the service is closed.
        /// </summary>
        public event EventHandler ScriptClosed;

        /// <summary>
        /// Occurs when the source video frame being rendered changes.
        /// </summary>
        public event EventHandler<FrameChangedEventArgs> FrameChanged;

        /// <summary>
        /// Occurs when a new Direct3D source render surface is created.
        /// </summary>
        public event EventHandler<NewRenderSurfaceEventArgs> NewSourceRenderSurface;

        /// <summary>
        /// Occurs when a new Direct3D preview render surface is created.
        /// </summary>
        public event EventHandler<NewRenderSurfaceEventArgs> NewPreviewRenderSurface;

        /// <summary>
        /// Occurs when a Direct3D surface has completed rendering.
        /// </summary>
        public event EventHandler<SurfaceRenderedEventArgs> SurfaceRendered;

        /// <summary>
        /// Occurs when asynchronous video playback has started.
        /// </summary>
        public event EventHandler VideoPlaybackStarted;

        /// <summary>
        /// Occurs when asynchronous video playback has stopped.
        /// </summary>
        public event EventHandler VideoPlaybackStopped;

        /// <summary>
        /// Base constructor for classes derived from the <see cref="ScriptVideoServiceBase"/> class.
        /// </summary>
        protected ScriptVideoServiceBase()
        {
            _playVideoTaskCancellationContext = new PlayVideoTaskCancellationContext();
        }

        /// <summary>
        /// Sets the window for presenting the Direct3D source and preview render surfaces.
        /// </summary>
        /// <param name="windowHandle">The handle of the window.</param>
        public abstract void SetPresentationWindow(IntPtr windowHandle);

        /// <summary>
        /// Applies a masking preview to the Direct3D source render surface.
        /// </summary>
        public abstract void ApplyMaskingPreviewToSourceRender();

        /// <summary>
        /// Removes a masking preview from the Direct3D source render surface.
        /// </summary>
        public abstract void RemoveMaskingPreviewFromSourceRender();

        /// <summary>
        /// Gets a reference to the <see cref="IScriptVideoContext">runtime context</see> of the service.
        /// </summary>
        /// <returns>The <see cref="IScriptVideoContext">runtime context</see> of the service.</returns>
        public abstract IScriptVideoContext GetContextReference();

        /// <summary>
        /// Loads an AviSynth script containing source video from a file into the service.
        /// </summary>
        /// <param name="scriptFileName">The file path of the AviSynth script.</param>
        /// <returns>A <see cref="bool"/> value indicating success or failure.</returns>
        public void LoadScriptFromFileSource(string scriptFileName)
        {
            if (string.IsNullOrWhiteSpace(scriptFileName))
            {
                throw new ArgumentNullException(nameof(scriptFileName));
            }

            var internalContext = InternalContext;
            if (internalContext.HasVideo || !string.IsNullOrWhiteSpace(internalContext.ScriptFileSource))
            {
                CloseScript();
            }

            if (!File.Exists(scriptFileName))
            {
                throw new FileNotFoundException("Can't find the AviSynth script", scriptFileName);
            }

            // Load script into unmanaged environment and initialize Direct3D and Direct2D source frame surfaces.
            LoadUnmanagedAviSynthScriptFromFile(scriptFileName);

            internalContext.RefreshOutputPreviewPixelSize();

            InitializeUnmanagedPreviewRenderSurface();

            if (NewSourceRenderSurface != null)
            {
                PushNewSourceRenderSurfaceToSubscribers();
            }

            if (NewPreviewRenderSurface != null)
            {
                PushNewPreviewRenderSurfaceToSubscribers();
            }

            SeekFrameCore(0, seekEvenIfCurrent: true, raiseEvents: true);

            ScriptOpened?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Finishes pending operations, closes the loaded script, releases resources
        /// and resets the runtime context.
        /// </summary>
        public void CloseScript()
        {
            if (InternalContext.IsVideoPlaying)
            {
                _playVideoTaskCancellationContext.SetCloseScript();
            }
            else
            {
                CloseScriptCore();
                ScriptClosed?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// Starts asynchronous video playback to source and preview Direct3D surfaces beginning at runtime context <see cref="IScriptVideoContext.FrameNumber"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> object representing the asynchronous operation.</returns>
        public async Task StartVideoPlayback()
        {
            _playVideoTaskCancellationContext.Reset();

            InternalContext.IsVideoPlaying = true;
            VideoPlaybackStarted?.Invoke(this, new EventArgs());

            IProgress<int> frameChangeProgress = new Progress<int>(OnFrameChanged);

            try
            {
                _ = await Task.Run(() => PlayVideo(frameChangeProgress));

                if (!_playVideoTaskCancellationContext.IsCancellationRequested || _playVideoTaskCancellationContext.CancellationReason == PlayVideoTaskCancellationReason.StopRequested)
                {
                    SeekFrame(0);
                }
            }
            catch (Exception ex)
            {
                // TODO: Is the service method the right place to catch the exception and notify the UI? Could this be handled in the View/ViewModel(s) calling this method?
                InternalContext.SystemDialogService.ShowErrorDialog("An exception occurred during video playback", "Script video service", ex);
            }

            PlayVideoTaskCancellationReason playVideoTaskCancellationReason = _playVideoTaskCancellationContext.CancellationReason;

            _playVideoTaskCancellationContext.Reset();

            InternalContext.IsVideoPlaying = false;
            VideoPlaybackStopped?.Invoke(this, new EventArgs());

            if (playVideoTaskCancellationReason == PlayVideoTaskCancellationReason.CloseScriptRequested)
            {
                CloseScriptCore();
                ScriptClosed?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// Pauses asynchronous video playback to source and preview Direct3D surfaces.
        /// </summary>
        public void PauseVideoPlayback()
        {
            if (InternalContext.IsVideoPlaying)
            {
                _playVideoTaskCancellationContext.SetPause();
            }
        }

        /// <summary>
        /// Stops asynchronous video playback and <see cref="SeekFrame(int)">seeks and renders the first video frame (0) to source and preview Direct3D surfaces</see>.
        /// </summary>
        public void StopVideoPlayback()
        {
            if (InternalContext.IsVideoPlaying)
            {
                _playVideoTaskCancellationContext.SetStop();
            }
            else
            {
                SeekFrame(0);
            }
        }

        /// <summary>
        /// Seeks a video frame from the loaded script and renders to source and preview Direct3D surfaces.
        /// </summary>
        /// <remarks>If successful, the runtime context <see cref="IScriptVideoContext.FrameNumber"/> property reflects the number of the sought frame.</remarks>
        /// <param name="frameNumber">The zero-based video frame number to seek and render.</param>
        public void SeekFrame(int frameNumber)
        {
            SeekFrameCore(frameNumber, seekEvenIfCurrent: false, raiseEvents: true);
        }

        /// <summary>
        /// Sets video resizing preview options such as resize mode, aspect ratio or pixel width and height.
        /// </summary>
        /// <param name="outputPreviewSize">A <see cref="VideoSizeOptions"/> structure specifying video resizing preview options</param>
        public void SetOutputPreviewSize(VideoSizeOptions outputPreviewSize)
        {
            var internalContext = InternalContext;
            if (internalContext.OutputPreviewSize != outputPreviewSize)
            {
                internalContext.SetOutputPreviewSizeInternal(outputPreviewSize);
            }

            if (internalContext.HasVideo)
            {
                internalContext.RefreshOutputPreviewPixelSize();

                InitializeUnmanagedPreviewRenderSurface();

                if (NewPreviewRenderSurface != null)
                {
                    PushNewPreviewRenderSurfaceToSubscribers();
                }

                RenderUnmanagedPreviewFrameSurface();
            }
        }

        /// <summary>
        /// Sets a collection of cropping segments to be rendered to the Direct3D preview surface.
        /// </summary>
        /// <param name="previewFrameCroppingSegments">
        /// A collection of cropping segments matching the current <see cref="IScriptVideoContext.FrameNumber"/>
        /// or an empty collection to clear a cropping preview from the Direct3D preview render surface.
        /// </param>
        public void SetPreviewFrameCroppingSegments(IEnumerable<SegmentModelBase> previewFrameCroppingSegments)
        {
            IEnumerable<SegmentKeyFrameLerpDataItem> croppingKeyFrameLerpDataItems = GetSegmentKeyFrameLerpDataForPreviewFrame(InternalContext.FrameNumber, previewFrameCroppingSegments);
            SetUnmanagedCroppingPreviewItems(croppingKeyFrameLerpDataItems);

            RenderUnmanagedPreviewFrameSurface();
        }

        /// <summary>
        /// Sets a collection of masking segments to be rendered to the Direct3D preview and <see cref="ApplyMaskingPreviewToSourceRender">optionally source</see> surfaces.
        /// </summary>
        /// <param name="previewFrameMaskingSegments">
        /// A collection of masking segments matching the current <see cref="IScriptVideoContext.FrameNumber"/>
        /// or an empty collection to clear a masking preview from the Direct3D render surfaces.
        /// </param>
        public void SetPreviewFrameMaskingSegments(IEnumerable<SegmentModelBase> previewFrameMaskingSegments)
        {
            IEnumerable<SegmentKeyFrameLerpDataItem> maskingKeyFrameLerpDataItems = GetSegmentKeyFrameLerpDataForPreviewFrame(InternalContext.FrameNumber, previewFrameMaskingSegments);
            SetUnmanagedMaskingPreviewItems(maskingKeyFrameLerpDataItems);

            RenderUnmanagedPreviewFrameSurface();
        }

        /// <summary>
        /// Core method for seeking a video frame from the loaded script and rendering to source and preview Direct3D surfaces.
        /// </summary>
        /// <param name="frameNumber">The zero-based video frame number to seek and render.</param>
        /// <param name="seekEvenIfCurrent">Whether to seek and render the video frame even if <paramref name="frameNumber"/> is the same value as runtime context <see cref="ScriptVideoContextBase.FrameNumber"/>.</param>
        /// <param name="raiseEvents">Whether to raise <see cref="FrameChanged"/> and <see cref="SurfaceRendered"/> events after seeking and rendering the frame.</param>
        /// <returns>A <see cref="bool"/> value indicating success or failure.</returns>
        protected void SeekFrameCore(int frameNumber, bool seekEvenIfCurrent, bool raiseEvents)
        {
            var internalContext = InternalContext;
            if (!internalContext.HasVideo)
            {
                throw new InvalidDataException("Script doesn't output a video");
            }

            if (frameNumber < 0 || frameNumber >= internalContext.VideoFrameCount)
            {
                throw new ArgumentOutOfRangeException(nameof(frameNumber));
            }

            if (frameNumber == internalContext.FrameNumber && !seekEvenIfCurrent)
            {
                return;
            }

            if (internalContext.Project?.Masking?.Shapes?.Count > 0)
            {
                IEnumerable<SegmentModelBase> filteredMaskingSegmentModels = FilterSegmentModelsByFrameNumber(frameNumber, internalContext.Project.Masking.Shapes);
                IEnumerable<SegmentKeyFrameLerpDataItem> maskingKeyFrameLerpDataItems = GetSegmentKeyFrameLerpDataForPreviewFrame(frameNumber, filteredMaskingSegmentModels);

                SetUnmanagedMaskingPreviewItems(maskingKeyFrameLerpDataItems);
            }

            if (internalContext.Project?.Cropping?.CropSegments?.Count > 0)
            {
                IEnumerable<SegmentModelBase> filteredCroppingSegmentModels = FilterSegmentModelsByFrameNumber(frameNumber, internalContext.Project.Cropping.CropSegments);
                IEnumerable<SegmentKeyFrameLerpDataItem> croppingKeyFrameLerpDataItems = GetSegmentKeyFrameLerpDataForPreviewFrame(frameNumber, filteredCroppingSegmentModels);

                SetUnmanagedCroppingPreviewItems(croppingKeyFrameLerpDataItems);
            }

            RenderUnmanagedFrameSurfaces(frameNumber);

            if (raiseEvents)
            {
                OnFrameChanged(frameNumber);
            }
        }

        /// <summary>
        /// Performs video playback to source and preview Direct3D surfaces beginning at runtime context <see cref="ScriptVideoContextBase.FrameNumber"/>.
        /// </summary>
        /// <remarks>Should be executed on a separate thread.</remarks>
        /// <param name="frameChangeProgress">An <see cref="IProgress{T}"/> instance to perform frame change logic in the <see cref="SynchronizationContext">context</see> of the service thread.</param>
        /// <returns>The frame number of last video frame to be played back.</returns>
        protected int PlayVideo(IProgress<int> frameChangeProgress)
        {
            var internalContext = InternalContext;

            // Calculate expected frame rate.
            long ticksPerFrame = TimeSpan.FromSeconds(1d / internalContext.VideoFramerate.PrecisionValue).Ticks;

            int frameNumber = internalContext.FrameNumber;
            int maxFrameNumber = internalContext.SeekableVideoFrameCount;

            // Loop till the last video frame or cancellation is requested, whichever comes first.
            while (frameNumber < maxFrameNumber && !_playVideoTaskCancellationContext.IsCancellationRequested)
            {
                // Get a time reference for measuring frame render duration.
                long renderStartTicks = DateTime.Now.Ticks;

                // Seek and render the next frame but defer raising events as they need to be raised in the context of the service thread.
                int frameNumberToSeek = frameNumber + 1;
                SeekFrameCore(frameNumberToSeek, seekEvenIfCurrent: false, raiseEvents: false);

                // Only increment if seek and render was successful
                frameNumber = frameNumberToSeek;

                // Handle frame change logic such as raising events in the context of the service thread.
                frameChangeProgress.Report(frameNumber);

                // Measure how long it took to render the frame.
                long renderDurationTicks = DateTime.Now.Ticks - renderStartTicks;

                // Calculate how long to delay the loop (sleep) in order to maintain expected frame rate.
                long sleepTime = Math.Max(0, ticksPerFrame - renderDurationTicks);
                if (sleepTime > 0)
                {
                    // Frame rendered within the time expected. Sleep before next loop to maintain expected frame rate.
                    Thread.Sleep(TimeSpan.FromTicks(sleepTime));
                }
                // Else, we're just on time or running late (QTGMC filter in AviSynth script?) so continue loop without delay.
            }

            return frameNumber; // The frame number of last video frame to be played back.
        }

        /// <summary>
        /// Core method for finishing pending operations, closing the loaded script,
        /// releasing resources and resetting the runtime context.
        /// </summary>
        protected virtual void CloseScriptCore()
        {
            NewSourceRenderSurface?.Invoke(this, new NewRenderSurfaceEventArgs(IntPtr.Zero));
            NewPreviewRenderSurface?.Invoke(this, new NewRenderSurfaceEventArgs(IntPtr.Zero));

            var internalContext = InternalContext;
            internalContext.IsVideoPlaying = false;
            internalContext.HasVideo = false;
            internalContext.VideoFrameSize = Size.Empty;
            internalContext.VideoFrameCount = 0;
            internalContext.SeekableVideoFrameCount = 0;
            internalContext.VideoDuration = TimeSpan.Zero;
            internalContext.VideoFramerate = new Fraction(1, 1);
            internalContext.SetFrameNumberInternal(0);
            internalContext.SetVideoPositionInternal(TimeSpan.Zero);
            internalContext.AspectRatio = new Ratio(1, 1, false);

            internalContext.SetOutputPreviewSizeInternal(
                new VideoSizeOptions()
                {
                    ResizeMode = VideoResizeMode.None,
                    PixelWidth = 0,
                    PixelHeight = 0
                }
            );
        }

        /// <summary>
        /// Updates the runtime context <see cref="ScriptVideoContextBase.FrameNumber"/> and <see cref="ScriptVideoContextBase.VideoPosition"/> property values
        /// and raises the <see cref="FrameChanged"/> and <see cref="SurfaceRendered"/> events.
        /// </summary>
        /// <param name="frameNumber">The current zero-based frame number of the video.</param>
        protected void OnFrameChanged(int frameNumber)
        {
            var internalContext = InternalContext;
            int previousFrameNumber = internalContext.FrameNumber;
            internalContext.SetFrameNumberInternal(frameNumber);
            internalContext.RefreshVideoPositionInternal();

            FrameChanged?.Invoke(this, new FrameChangedEventArgs(previousFrameNumber, frameNumber));
            SurfaceRendered?.Invoke(this, new SurfaceRenderedEventArgs(SurfaceRenderPipeline.Both));
        }

        /// <summary>
        /// Raises the <see cref="NewSourceRenderSurface"/> event
        /// with arguments containing a <see cref="IntPtr">pointer</see> to the unmanaged Direct3D source render surface.
        /// </summary>
        /// <param name="sourceRenderSurfacePtr">A <see cref="IntPtr">pointer</see> to the unmanaged Direct3D source render surface.</param>
        protected void OnNewSourceRenderSurface(IntPtr sourceRenderSurfacePtr)
        {
            NewSourceRenderSurface?.Invoke(this, new NewRenderSurfaceEventArgs(sourceRenderSurfacePtr));
        }

        /// <summary>
        /// Raises the <see cref="NewPreviewRenderSurface"/> event
        /// with arguments containing a <see cref="IntPtr">pointer</see> to the unmanaged Direct3D preview render surface.
        /// </summary>
        /// <param name="previewRenderSurfacePtr">A <see cref="IntPtr">pointer</see> to the unmanaged Direct3D preview render surface.</param>
        protected void OnNewPreviewRenderSurface(IntPtr previewRenderSurfacePtr)
        {
            NewPreviewRenderSurface?.Invoke(this, new NewRenderSurfaceEventArgs(previewRenderSurfacePtr));
        }

        /// <summary>
        /// Raises the <see cref="SurfaceRendered"/> event with arguments specifying which Direct3D surface was rendered.
        /// </summary>
        /// <param name="surfaceRenderPipeline">A <see cref="SurfaceRenderPipeline"/> enum value specifying which Direct3D surface was rendered.</param>
        protected void OnSurfaceRendered(SurfaceRenderPipeline surfaceRenderPipeline)
        {
            SurfaceRendered?.Invoke(this, new SurfaceRenderedEventArgs(surfaceRenderPipeline));
        }

        /// <summary>
        /// Gets a filtered collection of <see cref="SegmentModelBase">segments</see> from a given <see cref="SegmentModelCollection"/>
        /// where the specified frame number is within a segment's <see cref="SegmentModelBase.StartFrame">start</see> and <see cref="SegmentModelBase.EndFrame">end</see> frame range.
        /// </summary>
        /// <param name="frameNumber">The frame number to filter by.</param>
        /// <param name="segmentModelCollection">The <see cref="SegmentModelCollection"/> to filter.</param>
        /// <returns>An enumerable collection of <see cref="SegmentModelBase">segments</see> where the <see cref="SegmentModelBase.StartFrame">start</see> and <see cref="SegmentModelBase.EndFrame">end</see> frame range contains the <paramref name="frameNumber"/> parameter value.</returns>
        protected IEnumerable<SegmentModelBase> FilterSegmentModelsByFrameNumber(int frameNumber, SegmentModelCollection segmentModelCollection)
        {
            return segmentModelCollection.Where(segment => segment.IsFrameWithin(frameNumber));
        }

        /// <summary>
        /// Gets a collection of items containing data for performing linear interpolation of managed key frames
        /// when setting the content of the unmanaged Direct2D renderer's preview items cache.
        /// </summary>
        /// <param name="previewFrameNumber">The target frame number for creating interpolated unmanaged preview items.</param>
        /// <param name="segmentModelsFilteredByFrameNumber">The source collection of frame number filtered segment models.</param>
        /// <returns>A collection of data items for performing key frame linear interpolation.</returns>
        protected IEnumerable<SegmentKeyFrameLerpDataItem> GetSegmentKeyFrameLerpDataForPreviewFrame(int previewFrameNumber, IEnumerable<SegmentModelBase> segmentModelsFilteredByFrameNumber)
        {
            foreach (SegmentModelBase segmentModel in segmentModelsFilteredByFrameNumber)
            {
                KeyFrameModelBase keyFrameAtOrBefore, keyFrameAfter = null;
                double lerpAmount = 0d;

                int keyFrameIndex = segmentModel.KeyFrames.BinarySearch(previewFrameNumber);
                if (keyFrameIndex >= 0)
                {
                    // Exact match for frame number found
                    keyFrameAtOrBefore = segmentModel.KeyFrames[keyFrameIndex];
                }
                else
                {
                    // The index of the first key frame that has a frame number greater than or equal to previewFrameNumber
                    // or the value of segmentModel.KeyFrames.Count if no such key frame was found.
                    keyFrameIndex = ~keyFrameIndex;

                    keyFrameAtOrBefore = (keyFrameIndex > 0) ? segmentModel.KeyFrames[keyFrameIndex - 1] : segmentModel.KeyFrames[keyFrameIndex];
                    keyFrameAfter = (keyFrameIndex < segmentModel.KeyFrames.Count) ? segmentModel.KeyFrames[keyFrameIndex] : keyFrameAtOrBefore;

                    int frameRange = keyFrameAfter.FrameNumber - keyFrameAtOrBefore.FrameNumber;
                    if (frameRange > 0) // prevent double.NaN value as a result of division by zero
                    {
                        lerpAmount = (double)(previewFrameNumber - keyFrameAtOrBefore.FrameNumber) / frameRange;
                    }
                }

                yield return new SegmentKeyFrameLerpDataItem(segmentModel.TrackNumber, keyFrameAtOrBefore, keyFrameAfter, lerpAmount);
            }
        }

        /// <summary>
        /// Loads an AviSynth script from a file into the unmanaged AviSynth environment
        /// and initializes the unmanaged Direct3D source frame surface via interop.
        /// </summary>
        /// <param name="scriptFileName">The file path of the AviSynth script.</param>
        protected abstract void LoadUnmanagedAviSynthScriptFromFile(string scriptFileName);

        /// <summary>
        /// Creates and initializes the unmanaged Direct3D preview render surface.
        /// </summary>
        protected abstract void InitializeUnmanagedPreviewRenderSurface();

        /// <summary>
        /// Retrieves a pointer to the unmanaged Direct3D source render surface
        /// and pushes it to subscribers of the <see cref="NewSourceRenderSurface"/> event.
        /// </summary>
        protected abstract void PushNewSourceRenderSurfaceToSubscribers();

        /// <summary>
        /// Retrieves a pointer to the unmanaged Direct3D preview render surface
        /// and pushes it to subscribers of the <see cref="NewPreviewRenderSurface"/> event.
        /// </summary>
        protected abstract void PushNewPreviewRenderSurfaceToSubscribers();

        /// <summary>
        /// Renders unmanaged Direct3D source and preview surfaces for a given frame number.
        /// </summary>
        /// <param name="frameNumber">The source frame number to render.</param>
        protected abstract void RenderUnmanagedFrameSurfaces(int frameNumber);

        /// <summary>
        /// Renders the unmanaged Direct3D preview surface.
        /// </summary>
        protected abstract void RenderUnmanagedPreviewFrameSurface();

        /// <summary>
        /// Sets the content of the unmanaged Direct2D renderer's masking preview items cache via interop to
        /// frame data interpolated from masking segment key frame models.
        /// </summary>
        /// <param name="maskingKeyFrameLerpDataItems">A collection of <see cref="SegmentKeyFrameLerpDataItem">data items</see> for performing masking segment key frame linear interpolation.</param>
        protected abstract void SetUnmanagedMaskingPreviewItems(IEnumerable<SegmentKeyFrameLerpDataItem> maskingKeyFrameLerpDataItems);

        /// <summary>
        /// Sets the content of the unmanaged Direct2D renderer's cropping preview items cache via interop to
        /// frame data interpolated from cropping segment key frame models.
        /// </summary>
        /// <param name="croppingKeyFrameLerpDataItems">A collection of <see cref="SegmentKeyFrameLerpDataItem">data items</see> for performing cropping segment key frame linear interpolation.</param>
        protected abstract void SetUnmanagedCroppingPreviewItems(IEnumerable<SegmentKeyFrameLerpDataItem> croppingKeyFrameLerpDataItems);
    }
}
