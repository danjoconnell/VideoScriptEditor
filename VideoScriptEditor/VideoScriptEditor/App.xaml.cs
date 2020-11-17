using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Windows;
using System.Windows.Threading;
using VideoScriptEditor.Commands;
using VideoScriptEditor.PrismExtensions;
using VideoScriptEditor.Services;
using VideoScriptEditor.Services.Dialog;
using VideoScriptEditor.Services.ScriptVideo;
using VideoScriptEditor.Settings;
using VideoScriptEditor.Views;

namespace VideoScriptEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private const string AppSettingsFileName = "settings.json";
        private ApplicationSettings _applicationSettings;

        private SystemDialogService _systemDialogService;
        private ScriptVideoService _scriptVideoService;
        private ClipboardService _clipboardService;

        /// <inheritdoc/>
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        /// <inheritdoc/>
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            _applicationSettings = ApplicationSettings.LoadFromFile(AppDomain.CurrentDomain.BaseDirectory + AppSettingsFileName);
            containerRegistry.RegisterInstance<IApplicationSettings>(_applicationSettings);

            _systemDialogService = new SystemDialogService();
            containerRegistry.RegisterInstance<ISystemDialogService>(_systemDialogService);

            _scriptVideoService = new ScriptVideoService(_systemDialogService);
            containerRegistry.RegisterInstance<IScriptVideoService>(_scriptVideoService);

            _clipboardService = new ClipboardService();
            containerRegistry.RegisterInstance<IClipboardService>(_clipboardService);

            containerRegistry.RegisterSingleton<IProjectService, ProjectService>();

            containerRegistry.RegisterInstance<MonitoredUndo.IUndoService>(MonitoredUndo.UndoService.Current);
            containerRegistry.RegisterSingleton<MonitoredUndo.IChangeFactory, MonitoredUndo.ChangeFactory>();

            containerRegistry.RegisterSingleton<IApplicationCommands, ApplicationCommands>();
            containerRegistry.RegisterSingleton<ITimelineCommands, TimelineCommands>();

            containerRegistry.RegisterDialog<Views.Dialogs.InputValuePromptDialog, ViewModels.Dialogs.InputValuePromptDialogViewModel>();
            containerRegistry.RegisterDialog<Views.Dialogs.OutputVideoPropertiesDialog, ViewModels.Dialogs.OutputVideoPropertiesDialogViewModel>();

            containerRegistry.RegisterForNavigation<Views.Cropping.CroppingVideoOverlayView>();
            containerRegistry.RegisterForNavigation<Views.Cropping.CroppingRibbonGroupView>();
            containerRegistry.RegisterForNavigation<Views.Cropping.CroppingDetailsView>();
            containerRegistry.RegisterForNavigation<Views.Masking.MaskingVideoOverlayView>();
            containerRegistry.RegisterForNavigation<Views.Masking.MaskingRibbonGroupView>();
            containerRegistry.RegisterForNavigation<Views.Masking.MaskingDetailsView>();
        }

        /// <inheritdoc/>
        protected override void ConfigureRegionAdapterMappings(RegionAdapterMappings regionAdapterMappings)
        {
            base.ConfigureRegionAdapterMappings(regionAdapterMappings);

            if (regionAdapterMappings != null)
            {
                regionAdapterMappings.RegisterMapping(typeof(System.Windows.Controls.ContentPresenter), Container.Resolve<ContentPresenterRegionAdapter>());
                regionAdapterMappings.RegisterMapping(typeof(Fluent.RibbonTabItem), Container.Resolve<FluentRibbonTabItemRegionAdapter>());
            }
        }

        /// <inheritdoc/>
        protected override void ConfigureDefaultRegionBehaviors(IRegionBehaviorFactory regionBehaviors)
        {
            base.ConfigureDefaultRegionBehaviors(regionBehaviors);

            regionBehaviors.AddIfMissing(DependentViewRegionBehavior.BehaviorKey, typeof(DependentViewRegionBehavior));
        }

        /// <inheritdoc/>
        protected override void ConfigureViewModelLocator()
        {
            base.ConfigureViewModelLocator();

            ViewModelLocationProvider.Register<Views.Timeline.VideoTimelineView, ViewModels.Timeline.VideoTimelineViewModel>();
            ViewModelLocationProvider.Register<VideoDetailsView, ViewModels.VideoDetailsViewModel>();
        }

        /// <inheritdoc/>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // The ScriptVideoService instance must be safely disposed on exit, no matter what.
            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            base.OnInitialized();

            IntPtr mainWindowHandle = new System.Windows.Interop.WindowInteropHelper(MainWindow).EnsureHandle();

            try
            {
                _scriptVideoService.SetPresentationWindow(mainWindowHandle);
            }
            catch (Exception ex)
            {
                _systemDialogService.ShowErrorDialog("Closing due to an exception while attempting to set the Script Video Service presentation window", "Initialization Error", ex);
                Shutdown();
            }

            if (!_clipboardService.SetMonitorWindow(mainWindowHandle))
            {
                _systemDialogService.ShowErrorDialog("Closing due to a failure initializing the Clipboard service.", "Initialization Error");
                Shutdown();
            }
        }

        /// <inheritdoc/>
        protected override void OnExit(ExitEventArgs e)
        {
            _applicationSettings.SaveToFile(AppDomain.CurrentDomain.BaseDirectory + AppSettingsFileName);
            DisposeServiceInstances();

            base.OnExit(e);
        }

        /// <summary>
        /// Handles the <see cref="Application.DispatcherUnhandledException"/> event.
        /// </summary>
        /// <inheritdoc cref="DispatcherUnhandledExceptionEventHandler"/>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            DisposeServiceInstances();
        }

        /// <summary>
        /// Ensures that all local service instances
        /// are safely disposed.
        /// </summary>
        private void DisposeServiceInstances()
        {
            _scriptVideoService?.Dispose();
            _scriptVideoService = null;

            _systemDialogService = null;

            _clipboardService?.Dispose();
            _clipboardService = null;
        }
    }
}
