using System;

namespace VideoScriptEditor.Services.ScriptVideo
{
    /// <summary>
    /// Describes which Direct3D surface is being rendered by the <see cref="IScriptVideoService"/>.
    /// </summary>
    [Flags]
    public enum SurfaceRenderPipeline
    {
        /// <summary>
        /// The source video render surface.
        /// </summary>
        SourceVideo = 1,

        /// <summary>
        /// The output video preview render surface.
        /// </summary>
        OutputPreview = 2,

        /// <summary>
        /// The source and output video preview render surfaces.
        /// </summary>
        Both = SourceVideo | OutputPreview
    }
}
