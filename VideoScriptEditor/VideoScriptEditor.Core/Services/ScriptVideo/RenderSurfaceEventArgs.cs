using System;

namespace VideoScriptEditor.Services.ScriptVideo
{
    /// <summary>
    /// Provides data for <see cref="IScriptVideoService"/> events occurring when a new Direct3D render surface is created.
    /// </summary>
    public class NewRenderSurfaceEventArgs : EventArgs
    {
        /// <summary>
        /// A <see cref="IntPtr">pointer</see> to the unmanaged Direct3D render surface.
        /// </summary>
        public IntPtr RenderSurface { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="NewRenderSurfaceEventArgs"/> class.
        /// </summary>
        /// <param name="renderSurface">A <see cref="IntPtr">pointer</see> to the unmanaged Direct3D render surface.</param>
        public NewRenderSurfaceEventArgs(IntPtr renderSurface)
        {
            RenderSurface = renderSurface;
        }
    }

    /// <summary>
    /// Provides data for the <see cref="IScriptVideoService.SurfaceRendered"/> event.
    /// </summary>
    public class SurfaceRenderedEventArgs : EventArgs
    {
        /// <summary>
        /// A <see cref="SurfaceRenderPipeline"/> enum value specifying which Direct3D surface was rendered.
        /// </summary>
        public SurfaceRenderPipeline RenderPipeline { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="SurfaceRenderedEventArgs"/> class.
        /// </summary>
        /// <param name="renderPipeline">A <see cref="SurfaceRenderPipeline"/> enum value specifying which Direct3D surface was rendered.</param>
        public SurfaceRenderedEventArgs(SurfaceRenderPipeline renderPipeline)
        {
            RenderPipeline = renderPipeline;
        }
    }
}
