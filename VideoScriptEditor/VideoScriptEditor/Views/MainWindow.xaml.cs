using Prism.Regions;
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using VideoScriptEditor.Services.ScriptVideo;
using VideoScriptEditor.ViewModels;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.Views
{
    /// <summary>
    /// Main Window of the application.
    /// </summary>
    public partial class MainWindow : Fluent.RibbonWindow
    {
        private readonly IRegionManager _regionManager;

        /// <summary>
        /// Creates a new <see cref="MainWindow"/> instance.
        /// </summary>
        /// <param name="regionManager">The Prism <see cref="IRegionManager"/> instance.</param>
        /// <param name="scriptVideoService">
        /// The <see cref="IScriptVideoService"/> instance for processing and previewing edited video.
        /// </param>
        public MainWindow(IRegionManager regionManager, IScriptVideoService scriptVideoService) : base()
        {
            _regionManager = regionManager.RegisterViewWithRegion(RegionNames.VideoTimelineRegion, typeof(Timeline.VideoTimelineView))
                                          .RegisterViewWithRegion(RegionNames.VideoDetailsRegion, typeof(VideoDetailsView));

            InitializeComponent();

            // The VideoFramePresenter.RenderPipeline property
            // is dependent on the VideoFramePresenter.ScriptVideoService property being set first.
            _sourceVideoPresenter.ScriptVideoService = scriptVideoService;
            _sourceVideoPresenter.RenderPipeline = SurfaceRenderPipeline.SourceVideo;

            _outputVideoPreviewPresenter.ScriptVideoService = scriptVideoService;
            _outputVideoPreviewPresenter.RenderPipeline = SurfaceRenderPipeline.OutputPreview;
        }

        /// <inheritdoc/>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            // If set in XAML, this event will fire during initialization when tab control SelectedIndex is being changed from -1 (no selection) to 0 (the 'Video' tab).
            // Since the RegionManager will be initializing the views for the 'Video' tab during that stage, having this event fire during that process is undesirable.
            _subprojectTabControl.SelectionChanged += OnSubprojectTabSelectionChanged;
        }

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> routed event for the <see cref="_subprojectTabControl"/>.
        /// </summary>
        /// <remarks>
        /// Changes the active views in the <see cref="Region"/>s via the <see cref="IRegionManager"/> based on the tab selection.
        /// </remarks>
        /// <inheritdoc cref="SelectionChangedEventHandler"/>
        private void OnSubprojectTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Since the RegionManager will be initializing the views for the 'Video' tab during Window initialization, having this event fire during that process is undesirable.
            Debug.Assert(IsInitialized == true);

            if ((_subprojectTabControl.SelectedItem as TabItem)?.Tag is SubprojectType subprojectType)
            {
                if (subprojectType == SubprojectType.Video)
                {
                    IRegion videoDetailsRegion = _regionManager.Regions[RegionNames.VideoDetailsRegion];

                    object videoDetailsViewInstance = videoDetailsRegion.GetView(nameof(VideoDetailsView));
                    if (videoDetailsViewInstance != null)
                    {
                        videoDetailsRegion.Activate(videoDetailsViewInstance);
                    }
                    else
                    {
                        _regionManager.RegisterViewWithRegion(RegionNames.VideoDetailsRegion, typeof(VideoDetailsView));
                    }
                }

                string videoOverlayViewName = subprojectType switch
                {
                    SubprojectType.Cropping => nameof(Cropping.CroppingVideoOverlayView),
                    SubprojectType.Masking => nameof(Masking.MaskingVideoOverlayView),
                    _ => string.Empty,  // default
                };

                if (!string.IsNullOrEmpty(videoOverlayViewName))
                {
                    NavigationParameters navParams = new NavigationParameters()
                    {
                        { nameof(SubprojectType), subprojectType }
                    };

                    _regionManager.RequestNavigate(RegionNames.VideoOverlayRegion, videoOverlayViewName, navParams);
                }
                else
                {
                    IRegion region = _regionManager.Regions[RegionNames.VideoOverlayRegion];
                    object activeView = region.ActiveViews.FirstOrDefault();
                    if (activeView != null)
                    {
                        // Make sure OnNavigatedFrom is called on the active View's ViewModel for resource cleanup
                        NavigationContext navigationContext = new NavigationContext(region.NavigationService, null);
                        Prism.Common.MvvmHelpers.ViewAndViewModelAction(activeView,
                                                                        (Action<INavigationAware>)((n) => n.OnNavigatedFrom(navigationContext)));

                        region.Deactivate(activeView);
                    }
                }
            }
        }
    }
}
