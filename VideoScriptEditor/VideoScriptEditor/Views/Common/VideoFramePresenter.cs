using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using VideoScriptEditor.Services.ScriptVideo;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.Views.Common
{
    /// <summary>
    /// Represents an <see cref="Image"/> control that displays a video frame Direct3D render surface.
    /// </summary>
    public class VideoFramePresenter : Image
    {
        private readonly D3DImage _d3DImage;
        private bool _hasBackBuffer = false;

        /// <summary>
        /// Identifies the <see cref="RenderPipeline" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="RenderPipeline" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty RenderPipelineProperty =
                DependencyProperty.Register(
                        nameof(RenderPipeline),
                        typeof(SurfaceRenderPipeline?),
                        typeof(VideoFramePresenter),
                        new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnRenderPipelinePropertyChanged)),
                        new ValidateValueCallback(IsValidRenderPipelineValue));

        /// <summary>
        /// Gets or sets the target <see cref="SurfaceRenderPipeline"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="ScriptVideoService"/> property must have a valid value
        /// before setting this property.
        /// </remarks>
        public SurfaceRenderPipeline? RenderPipeline
        {
            get => (SurfaceRenderPipeline?)GetValue(RenderPipelineProperty);
            set => SetValue(RenderPipelineProperty, value);
        }

        /// <summary>
        /// <see cref="ValidateValueCallback"/> for the <see cref="RenderPipeline"/> property.
        /// </summary>
        /// <inheritdoc cref="ValidateValueCallback"/>
        private static bool IsValidRenderPipelineValue(object value)
        {
            return (SurfaceRenderPipeline?)value != SurfaceRenderPipeline.Both;
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="RenderPipeline"/> property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnRenderPipelinePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VideoFramePresenter presenter = (VideoFramePresenter)d;

            var scriptVideoService = presenter.ScriptVideoService;
            Debug.Assert(scriptVideoService != null);

            scriptVideoService.NewSourceRenderSurface -= presenter.OnNewRenderSurface;
            scriptVideoService.SurfaceRendered -= presenter.OnSurfaceRendered;
            scriptVideoService.NewPreviewRenderSurface -= presenter.OnNewRenderSurface;
            scriptVideoService.SurfaceRendered -= presenter.OnSurfaceRendered;

            switch ((SurfaceRenderPipeline?)e.NewValue)
            {
                case SurfaceRenderPipeline.SourceVideo:
                    scriptVideoService.NewSourceRenderSurface += presenter.OnNewRenderSurface;
                    scriptVideoService.SurfaceRendered += presenter.OnSurfaceRendered;
                    break;
                case SurfaceRenderPipeline.OutputPreview:
                    scriptVideoService.NewPreviewRenderSurface += presenter.OnNewRenderSurface;
                    scriptVideoService.SurfaceRendered += presenter.OnSurfaceRendered;
                    break;
            }
        }

        /// <summary>
        /// The <see cref="IScriptVideoService"/> instance for processing and previewing edited video.
        /// </summary>
        public IScriptVideoService ScriptVideoService { get; set; } = null;

        /// <summary>
        /// Creates a new <see cref="VideoFramePresenter"/> instance.
        /// </summary>
        public VideoFramePresenter() : base()
        {
            _d3DImage = new D3DImage();
            Source = _d3DImage;

            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Handles the <see cref="FrameworkElement.Unloaded"/> routed event.
        /// </summary>
        /// <remarks>
        /// Unsubscribes from <see cref="IScriptVideoService"/> events
        /// when this element is removed from within an element tree of loaded elements.
        /// </remarks>
        /// <inheritdoc cref="RoutedEventHandler"/>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (ScriptVideoService != null)
            {
                ScriptVideoService.SurfaceRendered -= OnSurfaceRendered;
                ScriptVideoService.NewSourceRenderSurface -= OnNewRenderSurface;
                ScriptVideoService.SurfaceRendered -= OnSurfaceRendered;
                ScriptVideoService.NewPreviewRenderSurface -= OnNewRenderSurface;
            }
        }

        /// <summary>
        /// Depending on the value of <see cref="RenderPipeline"/>, handles either
        /// the <see cref="IScriptVideoService.NewSourceRenderSurface"/> event
        /// or the <see cref="IScriptVideoService.NewPreviewRenderSurface"/> event.
        /// </summary>
        /// <inheritdoc cref="EventHandler{NewRenderSurfaceEventArgs}"/>
        private void OnNewRenderSurface(object sender, NewRenderSurfaceEventArgs e)
        {
            _d3DImage.Lock();
            _d3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, e.RenderSurface, true);
            _d3DImage.Unlock();

            _hasBackBuffer = e.RenderSurface != IntPtr.Zero;

            InvalidateD3DImage();
        }

        /// <summary>
        /// Handles the <see cref="IScriptVideoService.SurfaceRendered"/> event.
        /// </summary>
        /// <inheritdoc cref="EventHandler{SurfaceRenderedEventArgs}"/>
        private void OnSurfaceRendered(object sender, SurfaceRenderedEventArgs e)
        {
            Debug.Assert(RenderPipeline.HasValue);

            if (e.RenderPipeline.HasFlag(RenderPipeline.Value))
            {
                InvalidateD3DImage();
            }
        }

        /// <summary>
        /// Invalidates the entire <see cref="D3DImage"/> area.
        /// </summary>
        /// <remarks>
        /// Based on sample code from
        /// http://jmorrill.hjtcentral.com/Home/tabid/428/EntryId/437/Direct3D-10-11-Direct2D-in-WPF.aspx
        /// </remarks>
        private void InvalidateD3DImage()
        {
            if (_hasBackBuffer)
            {
                _d3DImage.Lock();
                _d3DImage.AddDirtyRect(new Int32Rect()
                {
                    X = 0,
                    Y = 0,
                    Height = _d3DImage.PixelHeight,
                    Width = _d3DImage.PixelWidth
                });
                _d3DImage.Unlock();
            }
        }
    }
}
