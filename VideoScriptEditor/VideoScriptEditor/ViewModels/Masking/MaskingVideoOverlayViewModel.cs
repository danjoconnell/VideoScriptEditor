using MonitoredUndo;
using Prism.Commands;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Commands;
using VideoScriptEditor.Geometry;
using VideoScriptEditor.Services;
using VideoScriptEditor.Services.ScriptVideo;
using VideoScriptEditor.ViewModels.Masking.Shapes;
using VideoScriptEditor.ViewModels.Timeline;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.ViewModels.Masking
{
    /// <summary>
    /// View Model encapsulating presentation logic for the Masking Video Overlay view.
    /// </summary>
    /// <remarks>Implements <see cref="IMaskingViewModel"/>.</remarks>
    public class MaskingVideoOverlayViewModel : VideoOverlayViewModelBase, IMaskingViewModel
    {
        private readonly Services.Dialog.ISystemDialogService _systemDialogService;
        private MaskShapeResizeMode? _shapeResizeMode = null;
        private MaskShapeViewModelBase _selectedSegment = null;
        private readonly ObservableCollection<Enum> _addableSegmentTypes;

        /// <inheritdoc cref="IMaskingViewModel.ShapeResizeModeChanged"/>
        public event EventHandler ShapeResizeModeChanged;

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

                    SetProperty(ref _selectedSegment, (MaskShapeViewModelBase)value, OnSelectedSegmentChanged);
                }
            }
        }

        /// <inheritdoc cref="VideoOverlayViewModelBase.AddableSegmentTypes"/>
        public override ReadOnlyObservableCollection<Enum> AddableSegmentTypes => new ReadOnlyObservableCollection<Enum>(_addableSegmentTypes);

        /// <inheritdoc cref="TimelineSegmentProvidingViewModelBase.SegmentModels"/>
        public override SegmentModelCollection SegmentModels => Project?.Masking?.Shapes;

        /// <inheritdoc cref="TimelineSegmentProvidingViewModelBase.SegmentViewModelFactory"/>
        public override ISegmentViewModelFactory SegmentViewModelFactory { get; }

        /// <inheritdoc cref="IMaskingViewModel.ShapeResizeMode"/>
        public MaskShapeResizeMode? ShapeResizeMode
        {
            get => _shapeResizeMode;
            set
            {
                if (SetProperty(ref _shapeResizeMode, value))
                {
                    ShapeResizeModeChanged?.Invoke(this, new EventArgs());
                }
            }
        }

        /// <summary>
        /// Command for setting the value of the <see cref="ShapeResizeMode"/> property.
        /// </summary>
        public DelegateCommand<MaskShapeResizeMode?> SetShapeResizeModeCommand { get; }

        /// <summary>
        /// Command for flipping the <see cref="SelectedSegment">selected masking shape</see>
        /// along the specified <see cref="Axis"/>.
        /// </summary>
        public DelegateCommand<Axis?> FlipSelectedShapeCommand { get; }

        /// <summary>
        /// Creates a new <see cref="MaskingVideoOverlayViewModel"/> instance.
        /// </summary>
        /// <inheritdoc cref="VideoOverlayViewModelBase(IScriptVideoService, IUndoService, IChangeFactory, IApplicationCommands, IProjectService)"/>
        /// <param name="systemDialogService">The <see cref="Services.Dialog.ISystemDialogService"/> instance for forwarding messages to the UI.</param>
        /// <param name="clipboardService">The <see cref="IClipboardService"/> instance providing access to the system clipboard.</param>
        public MaskingVideoOverlayViewModel(IScriptVideoService scriptVideoService, IUndoService undoService, IChangeFactory undoChangeFactory, IApplicationCommands applicationCommands, IProjectService projectService, Services.Dialog.ISystemDialogService systemDialogService, IClipboardService clipboardService) : base(scriptVideoService, undoService, undoChangeFactory, applicationCommands, projectService)
        {
            SegmentViewModelFactory = new MaskShapeViewModelFactory(ScriptVideoContext, undoService, undoChangeFactory, GetUndoRoot(), clipboardService);
            _systemDialogService = systemDialogService;

            _addableSegmentTypes = new ObservableCollection<Enum>
            {
                { PolygonShapeType.IsoscelesTriangle },
                { PolygonShapeType.RightTriangle },
                { MaskShapeType.Rectangle },
                { MaskShapeType.Ellipse }
            };

            SetShapeResizeModeCommand = new DelegateCommand<MaskShapeResizeMode?>(
                executeMethod: (shapeResizeMode) => ShapeResizeMode = shapeResizeMode,
                canExecuteMethod: CanSetShapeResizeModeCommandExecute
            ).ObservesProperty(() => SelectedSegment.CanBeEdited);

            FlipSelectedShapeCommand = new DelegateCommand<Axis?>(
                executeMethod: (axis) => _selectedSegment.Flip(axis.Value),
                canExecuteMethod: (axis) => _selectedSegment?.CanBeEdited == true
            ).ObservesProperty(() => SelectedSegment.CanBeEdited);
        }

        /// <inheritdoc cref="VideoOverlayViewModelBase.IsNavigationTarget(NavigationContext)"/>
        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return navigationContext.Parameters.GetValue<SubprojectType>(nameof(SubprojectType)) == SubprojectType.Masking;
        }

        /// <inheritdoc cref="TimelineSegmentProvidingViewModelBase.RefreshActiveSegments"/>
        public override void RefreshActiveSegments()
        {
            base.RefreshActiveSegments();

            RefreshScriptVideoMaskingPreview();
        }

        /// <inheritdoc cref="TimelineSegmentProvidingViewModelBase.OnSelectedSegmentChanged"/>
        protected override void OnSelectedSegmentChanged()
        {
            base.OnSelectedSegmentChanged();

            if (_selectedSegment != null)
            {
                if (!_shapeResizeMode.HasValue || !_selectedSegment.SupportsResizeMode(_shapeResizeMode.Value))
                {
                    ShapeResizeMode = MaskShapeResizeMode.Bounds;
                }
            }
            else
            {
                ShapeResizeMode = null;
            }

            SetShapeResizeModeCommand.RaiseCanExecuteChanged();
            FlipSelectedShapeCommand.RaiseCanExecuteChanged();
        }

        /// <inheritdoc cref="TimelineSegmentProvidingViewModelBase.OnSelectedSegmentInstancePropertyChanged(object, PropertyChangedEventArgs)"/>
        protected override void OnSelectedSegmentInstancePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnSelectedSegmentInstancePropertyChanged(sender, e);

            if (_selectedSegment.CanBeEdited)
            {
                if (e.PropertyName != nameof(SegmentViewModelBase.ActiveKeyFrame) && e.PropertyName != nameof(SegmentViewModelBase.CanBeEdited))
                {
                    RefreshScriptVideoMaskingPreview();
                }
            }
        }

        /// <summary>
        /// Determines whether the value of the <see cref="ShapeResizeMode"/> property can be set
        /// via the <see cref="SetShapeResizeModeCommand"/>.
        /// </summary>
        /// <remarks>CanExecute delegate method for the <see cref="SetShapeResizeModeCommand"/>.</remarks>
        /// <param name="maskShapeResizeMode">The new value for the <see cref="ShapeResizeMode"/> property.</param>
        /// <returns>True if the <see cref="ShapeResizeMode"/> property can be set to the new value, otherwise False.</returns>
        private bool CanSetShapeResizeModeCommandExecute(MaskShapeResizeMode? maskShapeResizeMode)
        {
            return maskShapeResizeMode.HasValue
                   && _selectedSegment?.CanBeEdited == true
                   && _selectedSegment.SupportsResizeMode(maskShapeResizeMode.Value);
        }

        /// <summary>
        /// Refreshes the <see cref="IScriptVideoService"/> instance's masking preview
        /// of the <see cref="TimelineSegmentProvidingViewModelBase.ActiveSegments"/>.
        /// </summary>
        private void RefreshScriptVideoMaskingPreview()
        {
            IEnumerable<Models.SegmentModelBase> activeSegmentModels = ActiveSegments.Select(shapeVM => shapeVM.Model);

            try
            {
                _scriptVideoService.SetPreviewFrameMaskingSegments(activeSegmentModels);
            }
            catch (Exception ex)
            {
                _systemDialogService.ShowErrorDialog("An exception occurred while refreshing the masking preview", "Masking Error", ex);
            }
        }
    }
}
