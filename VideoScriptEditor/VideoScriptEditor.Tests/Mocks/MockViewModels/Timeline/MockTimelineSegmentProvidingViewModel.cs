using MonitoredUndo;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Services.ScriptVideo;
using VideoScriptEditor.ViewModels.Timeline;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.Tests.Mocks.MockViewModels.Timeline
{
    public class MockTimelineSegmentProvidingViewModel : TimelineSegmentProvidingViewModelBase
    {
        private MockSegmentViewModel _selectedSegment = null;

        public override SegmentModelCollection SegmentModels { get; }
        public override ISegmentViewModelFactory SegmentViewModelFactory { get; }

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

                    SetProperty(ref _selectedSegment, (MockSegmentViewModel)value, OnSelectedSegmentChanged);
                }
            }
        }

        public MockTimelineSegmentProvidingViewModel(SegmentModelCollection segmentModels, IScriptVideoService scriptVideoService, IUndoService undoService, IChangeFactory undoChangeFactory, Services.IClipboardService clipboardService, Commands.IApplicationCommands applicationCommands) : base(scriptVideoService, undoService, undoChangeFactory, applicationCommands)
        {
            SegmentModels = segmentModels;

            SegmentViewModelFactory = new MockSegmentViewModelFactory(ScriptVideoContext, undoService, undoChangeFactory, GetUndoRoot(), clipboardService);

            for (int i = 0; i < SegmentModels.Count; i++)
            {
                SegmentViewModels.Add(SegmentViewModelFactory.CreateSegmentViewModel(SegmentModels[i]));
            }

            SegmentModels.CollectionChanged += OnSegmentModelsCollectionChanged;
            SegmentViewModels.CollectionChanged += OnSegmentViewModelsCollectionChanged;
            SubscribeCommonEventsAndCommands();

            RefreshActiveSegmentsForFrame(ScriptVideoContext.FrameNumber);
        }
    }
}
