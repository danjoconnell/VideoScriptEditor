using MonitoredUndo;
using Prism.Commands;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Commands;
using VideoScriptEditor.Services;
using VideoScriptEditor.Services.ScriptVideo;
using VideoScriptEditor.ViewModels.Timeline;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.ViewModels.Cropping
{
    /// <summary>
    /// View Model encapsulating presentation logic for the Cropping Video Overlay view.
    /// </summary>
    /// <remarks>Implements <see cref="ICroppingViewModel"/>.</remarks>
    public class CroppingVideoOverlayViewModel : VideoOverlayViewModelBase, ICroppingViewModel
    {
        private readonly Services.Dialog.ISystemDialogService _systemDialogService;
        private CropSegmentViewModel _selectedSegment = null;
        private readonly ObservableCollection<Enum> _addableSegmentTypes;
        private CropAdjustmentHandleMode _adjustmentHandleMode = CropAdjustmentHandleMode.Resize;

        /// <inheritdoc cref="TimelineSegmentProvidingViewModelBase.SelectedSegment"/>
        public override SegmentViewModelBase SelectedSegment
        {
            get => _selectedSegment;
            set
            {
                if (_selectedSegment != value)
                {
                    Debug.Assert(value == null || _activeSegmentDictionary.ContainsValue(value));

                    if (_selectedSegment != null)
                    {
                        _selectedSegment.PropertyChanged -= OnSelectedSegmentInstancePropertyChanged;
                        _selectedSegment.IsSelected = false;
                    }

                    SetProperty(ref _selectedSegment, (CropSegmentViewModel)value, OnSelectedSegmentChanged);
                }
            }
        }

        /// <inheritdoc cref="VideoOverlayViewModelBase.AddableSegmentTypes"/>
        public override ReadOnlyObservableCollection<Enum> AddableSegmentTypes => new ReadOnlyObservableCollection<Enum>(_addableSegmentTypes);

        /// <inheritdoc cref="TimelineSegmentProvidingViewModelBase.SegmentModels"/>
        public override SegmentModelCollection SegmentModels => Project?.Cropping?.CropSegments;

        /// <inheritdoc cref="TimelineSegmentProvidingViewModelBase.SegmentViewModelFactory"/>
        public override ISegmentViewModelFactory SegmentViewModelFactory { get; }

        /// <inheritdoc cref="ICroppingViewModel.AdjustmentHandleMode"/>
        public CropAdjustmentHandleMode AdjustmentHandleMode
        {
            get => _adjustmentHandleMode;
            set => SetProperty(ref _adjustmentHandleMode, value);
        }

        /// <summary>
        /// Command for centering the <see cref="SelectedSegment">selected segment</see>
        /// horizontally.
        /// </summary>
        public DelegateCommand CenterSegmentHorizontallyCommand { get; }

        /// <summary>
        /// Command for centering the <see cref="SelectedSegment">selected segment</see>
        /// vertically.
        /// </summary>
        public DelegateCommand CenterSegmentVerticallyCommand { get; }

        /// <summary>
        /// Creates a new <see cref="CroppingVideoOverlayViewModel"/> instance.
        /// </summary>
        /// <inheritdoc cref="VideoOverlayViewModelBase(IScriptVideoService, IUndoService, IChangeFactory, IApplicationCommands, IProjectService)"/>
        /// <param name="systemDialogService">The <see cref="Services.Dialog.ISystemDialogService"/> instance for forwarding messages to the UI.</param>
        /// <param name="clipboardService">The <see cref="IClipboardService"/> instance providing access to the system clipboard.</param>
        public CroppingVideoOverlayViewModel(IScriptVideoService scriptVideoService, IUndoService undoService, IChangeFactory undoChangeFactory, IApplicationCommands applicationCommands, IProjectService projectService, Services.Dialog.ISystemDialogService systemDialogService, IClipboardService clipboardService) : base(scriptVideoService, undoService, undoChangeFactory, applicationCommands, projectService)
        {
            SegmentViewModelFactory = new CropSegmentViewModelFactory(ScriptVideoContext, undoService, undoChangeFactory, GetUndoRoot(), clipboardService);
            _systemDialogService = systemDialogService;

            _addableSegmentTypes = new ObservableCollection<Enum>
            {
                { CropSegmentType.Crop }
            };

            CenterSegmentHorizontallyCommand = new DelegateCommand(
                executeMethod: () => _selectedSegment?.CenterHorizontally(),
                canExecuteMethod: () => _selectedSegment?.CanBeEdited == true
            ).ObservesProperty(() => SelectedSegment.CanBeEdited);

            CenterSegmentVerticallyCommand = new DelegateCommand(
                executeMethod: () => _selectedSegment?.CenterVertically(),
                canExecuteMethod: () => _selectedSegment?.CanBeEdited == true
            ).ObservesProperty(() => SelectedSegment.CanBeEdited);
        }

        /// <inheritdoc cref="VideoOverlayViewModelBase.OnNavigatedTo(NavigationContext)"/>
        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            if (ScriptVideoContext.HasVideo)
            {
                try
                {
                    _scriptVideoService.ApplyMaskingPreviewToSourceRender();
                }
                catch (Exception ex)
                {
                    _systemDialogService.ShowErrorDialog("An exception occurred while overlaying a masking preview", "Cropping Error", ex);
                }
            }
        }

        /// <inheritdoc cref="VideoOverlayViewModelBase.IsNavigationTarget(NavigationContext)"/>
        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return navigationContext.Parameters.GetValue<SubprojectType>(nameof(SubprojectType)) == SubprojectType.Cropping;
        }

        /// <inheritdoc cref="VideoOverlayViewModelBase.OnNavigatedFrom(NavigationContext)"/>
        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            if (ScriptVideoContext.HasVideo)
            {
                try
                {
                    _scriptVideoService.RemoveMaskingPreviewFromSourceRender();
                }
                catch (Exception ex)
                {
                    _systemDialogService.ShowErrorDialog("An exception occurred while removing the masking preview overlay", "Cropping Error", ex);
                }
            }

            base.OnNavigatedFrom(navigationContext);
        }

        /// <inheritdoc cref="TimelineSegmentProvidingViewModelBase.RefreshActiveSegments"/>
        public override void RefreshActiveSegments()
        {
            base.RefreshActiveSegments();

            _scriptVideoService.SetPreviewFrameCroppingSegments(
                ActiveSegments.Select(segmentVM => segmentVM.Model)
            );
        }

        /// <inheritdoc cref="TimelineSegmentProvidingViewModelBase.OnSelectedSegmentInstancePropertyChanged(object, PropertyChangedEventArgs)"/>
        protected override void OnSelectedSegmentInstancePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnSelectedSegmentInstancePropertyChanged(sender, e);

            if (_selectedSegment.CanBeEdited)
            {
                if (e.PropertyName != nameof(SegmentViewModelBase.ActiveKeyFrame) && e.PropertyName != nameof(SegmentViewModelBase.CanBeEdited))
                {
                    _scriptVideoService.SetPreviewFrameCroppingSegments(
                        ActiveSegments.Select(segmentVM => segmentVM.Model)
                    );
                }
            }
        }
    }
}
