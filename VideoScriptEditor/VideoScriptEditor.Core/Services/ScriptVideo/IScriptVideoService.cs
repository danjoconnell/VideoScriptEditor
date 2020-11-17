using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VideoScriptEditor.Models;

namespace VideoScriptEditor.Services.ScriptVideo
{
    /// <summary>
    /// Abstraction of a service for processing video from an AviSynth script
    /// and previewing the resulting edited video through a Direct2D renderer.
    /// </summary>
    public interface IScriptVideoService
    {
        /// <summary>
        /// Occurs when an AviSynth script is opened for processing by the service.
        /// </summary>
        event EventHandler ScriptOpened;

        /// <summary>
        /// Occurs when an AviSynth script that was being processed by the service is closed.
        /// </summary>
        event EventHandler ScriptClosed;

        /// <summary>
        /// Occurs when the source video frame being rendered changes.
        /// </summary>
        event EventHandler<FrameChangedEventArgs> FrameChanged;

        /// <summary>
        /// Occurs when a new Direct3D source render surface is created.
        /// </summary>
        event EventHandler<NewRenderSurfaceEventArgs> NewSourceRenderSurface;

        /// <summary>
        /// Occurs when a new Direct3D preview render surface is created.
        /// </summary>
        event EventHandler<NewRenderSurfaceEventArgs> NewPreviewRenderSurface;

        /// <summary>
        /// Occurs when a Direct3D surface has completed rendering.
        /// </summary>
        event EventHandler<SurfaceRenderedEventArgs> SurfaceRendered;

        /// <summary>
        /// Occurs when asynchronous video playback has started.
        /// </summary>
        event EventHandler VideoPlaybackStarted;

        /// <summary>
        /// Occurs when asynchronous video playback has stopped.
        /// </summary>
        event EventHandler VideoPlaybackStopped;

        /// <summary>
        /// Gets a reference to the <see cref="IScriptVideoContext">runtime context</see> of the service.
        /// </summary>
        /// <returns><see cref="IScriptVideoContext">runtime context</see> of the service.</returns>
        IScriptVideoContext GetContextReference();

        /// <summary>
        /// Loads an AviSynth script containing source video from a file into the service.
        /// </summary>
        /// <param name="scriptFileName">The file path of the AviSynth script.</param>
        void LoadScriptFromFileSource(string scriptFileName);

        /// <summary>
        /// Finishes pending operations, closes the loaded script, releases resources
        /// and resets the runtime context.
        /// </summary>
        void CloseScript();

        /// <summary>
        /// Sets video resizing preview options such as resize mode, aspect ratio or pixel width and height.
        /// </summary>
        /// <param name="outputPreviewSize">A <see cref="VideoSizeOptions"/> structure specifying video resizing preview options</param>
        void SetOutputPreviewSize(VideoSizeOptions outputPreviewSize);

        /// <summary>
        /// Sets the window for presenting the Direct3D source and preview render surfaces.
        /// </summary>
        /// <param name="windowHandle">The handle of the window.</param>
        void SetPresentationWindow(IntPtr windowHandle);

        /// <summary>
        /// Seeks a video frame from the loaded script and renders to source and preview Direct3D surfaces.
        /// </summary>
        /// <remarks>If successful, the runtime context <see cref="IScriptVideoContext.FrameNumber"/> property reflects the number of the sought frame.</remarks>
        /// <param name="frameNumber">The zero-based video frame number to seek and render.</param>
        void SeekFrame(int frameNumber);

        /// <summary>
        /// Starts asynchronous video playback to source and preview Direct3D surfaces beginning at runtime context <see cref="IScriptVideoContext.FrameNumber"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> object representing the asynchronous operation.</returns>
        Task StartVideoPlayback();

        /// <summary>
        /// Pauses asynchronous video playback to source and preview Direct3D surfaces.
        /// </summary>
        void PauseVideoPlayback();

        /// <summary>
        /// Stops asynchronous video playback and <see cref="SeekFrame(int)">seeks and renders the first video frame (0) to source and preview Direct3D surfaces</see>.
        /// </summary>
        void StopVideoPlayback();

        /// <summary>
        /// Sets a collection of masking segments to be rendered to the Direct3D preview and <see cref="ApplyMaskingPreviewToSourceRender">optionally source</see> surfaces.
        /// </summary>
        /// <param name="previewFrameMaskingSegments">
        /// A collection of masking segments matching the current <see cref="IScriptVideoContext.FrameNumber"/>
        /// or an empty collection to clear a masking preview from the Direct3D render surfaces.
        /// </param>
        void SetPreviewFrameMaskingSegments(IEnumerable<SegmentModelBase> previewFrameMaskingSegments);

        /// <summary>
        /// Sets a collection of cropping segments to be rendered to the Direct3D preview surface.
        /// </summary>
        /// <param name="previewFrameCroppingSegments">
        /// A collection of cropping segments matching the current <see cref="IScriptVideoContext.FrameNumber"/>
        /// or an empty collection to clear a cropping preview from the Direct3D preview render surface.
        /// </param>
        void SetPreviewFrameCroppingSegments(IEnumerable<SegmentModelBase> previewFrameCroppingSegments);

        /// <summary>
        /// Applies a masking preview to the Direct3D source render surface.
        /// </summary>
        void ApplyMaskingPreviewToSourceRender();

        /// <summary>
        /// Removes a masking preview from the Direct3D source render surface.
        /// </summary>
        void RemoveMaskingPreviewFromSourceRender();
    }
}
