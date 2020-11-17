using MonitoredUndo;
using Moq;
using Prism.Services.Dialogs;
using System;
using System.Linq;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Commands;
using VideoScriptEditor.Services;
using VideoScriptEditor.Services.ScriptVideo;
using VideoScriptEditor.Tests.Mocks.MockModels;
using VideoScriptEditor.Tests.Mocks.MockViewModels.Timeline;
using Xunit;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.ViewModels.Timeline.Tests
{
    public class VideoTimelineViewModelTests : IDisposable
    {
        private Mock<IScriptVideoService> _scriptVideoServiceMock;
        private Mock<IScriptVideoContext> _scriptVideoContextMock;
        private IUndoService _undoService;
        private IChangeFactory _undoChangeFactory;
        private UndoRoot _undoRoot;
        private Mock<IClipboardService> _clipboardServiceMock;
        private Mock<IDialogService> _dialogServiceMock;
        private TimelineCommands _timelineCommands;
        private ApplicationCommands _applicationCommands;
        private MockTimelineSegmentProvidingViewModel _timelineSegmentProvidingViewModel;
        private int _currentScriptVideoFrameNumber;

        private VideoTimelineViewModel _viewModel;

        [Fact]
        public void CanMoveTrackSegmentTest()
        {
            SetupViewModel(useRealUndoService: false);

            SegmentViewModelBase segmentToMove = _timelineSegmentProvidingViewModel.SegmentViewModels[6];

            Assert.True(
                _viewModel.CanMoveTrackSegment(segmentToMove, 2, 0)
            );

            Assert.False(
                _viewModel.CanMoveTrackSegment(segmentToMove, 1, 1)
            );

            segmentToMove = _timelineSegmentProvidingViewModel.SegmentViewModels[5];
            Assert.True(
                _viewModel.CanMoveTrackSegment(segmentToMove, 1, 1)
            );
        }

        [Theory]
        [InlineData(0, 0, 6, false, 2, 0)]
        [InlineData(0, 0, 0, true, 1, 40)]
        [InlineData(28, 1, 6, true, 2, 0)]
        public void MoveTrackSegmentTest(int preTestFrameNumber, int preTestTrackIndex, int segmentViewModelIndex, bool isSelectedSegmentPreMove, int newTrackNumber, int newStartFrame)
        {
            SetupViewModel(useRealUndoService: true);

            IScriptVideoContext svc = _scriptVideoContextMock.Object;
            if (svc.FrameNumber != preTestFrameNumber)
            {
                svc.FrameNumber = preTestFrameNumber;
            }

            _viewModel.SelectedTrack = _viewModel.TimelineTrackCollection[preTestTrackIndex];

            SegmentViewModelBase segmentToMove = _timelineSegmentProvidingViewModel.SegmentViewModels[segmentViewModelIndex];

            Assert.Equal(isSelectedSegmentPreMove, segmentToMove == _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);

            int originalTrackNumber = segmentToMove.TrackNumber;
            int originalStartFrame = segmentToMove.StartFrame;
            int originalEndFrame = segmentToMove.EndFrame;
            int newEndFrame = newStartFrame + segmentToMove.FrameDuration - 1;

            // Key frame tests
            int startFrameOffset = newStartFrame - originalStartFrame;
            int[] originalKeyFrameNumbers = segmentToMove.KeyFrameViewModels.Select(kfvm => kfvm.FrameNumber).ToArray();

            _viewModel.MoveTrackSegment(segmentToMove, newTrackNumber, newStartFrame);
            Assert.Equal(newTrackNumber, segmentToMove.TrackNumber);
            Assert.Equal(newStartFrame, segmentToMove.StartFrame);
            Assert.Equal(newEndFrame, segmentToMove.EndFrame);

            Assert.Equal(1, _viewModel.TimelineSegments.Count(segment => segment == segmentToMove));

            Assert.DoesNotContain(segmentToMove, _viewModel.TimelineTrackCollection[originalTrackNumber].TrackSegments);
            Assert.Contains(segmentToMove, _viewModel.TimelineTrackCollection[newTrackNumber].TrackSegments);

            // Key frame tests
            for (int i = 0; i < segmentToMove.KeyFrameViewModels.Count; i++)
            {
                Assert.Equal(originalKeyFrameNumbers[i] + startFrameOffset, segmentToMove.KeyFrameViewModels[i].FrameNumber);
            }

            if (isSelectedSegmentPreMove)
            {
                Assert.False(segmentToMove == _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);
            }

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalTrackNumber, segmentToMove.TrackNumber);
            Assert.Equal(originalStartFrame, segmentToMove.StartFrame);
            Assert.Equal(originalEndFrame, segmentToMove.EndFrame);

            Assert.Equal(1, _viewModel.TimelineSegments.Count(segment => segment == segmentToMove));

            Assert.DoesNotContain(segmentToMove, _viewModel.TimelineTrackCollection[newTrackNumber].TrackSegments);
            Assert.Contains(segmentToMove, _viewModel.TimelineTrackCollection[originalTrackNumber].TrackSegments);

            // Key frame tests
            for (int i = 0; i < segmentToMove.KeyFrameViewModels.Count; i++)
            {
                Assert.Equal(originalKeyFrameNumbers[i], segmentToMove.KeyFrameViewModels[i].FrameNumber);
            }

            Assert.Equal(isSelectedSegmentPreMove, segmentToMove == _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);
        }

        [Fact]
        public void RemoveTrackCommandTest()
        {
            SetupViewModel(useRealUndoService: true);

            Assert.Equal(3, _viewModel.TimelineTrackCollection.Count);
            Assert.Equal(10, _viewModel.TimelineSegments.Count);

            IVideoTimelineTrackViewModel trackToRemove = _viewModel.TimelineTrackCollection[0];

            _viewModel.RemoveTrackCommand.Execute(trackToRemove);

            Assert.Equal(2, _viewModel.TimelineTrackCollection.Count);

            Assert.Equal(5, _viewModel.TimelineSegments.Count);
            Assert.Equal(0, _viewModel.TimelineSegments[0].TrackNumber);
            Assert.Equal(0, _viewModel.TimelineSegments[1].TrackNumber);
            Assert.Equal(0, _viewModel.TimelineSegments[2].TrackNumber);
            Assert.Equal(1, _viewModel.TimelineSegments[3].TrackNumber);
            Assert.Equal(1, _viewModel.TimelineSegments[4].TrackNumber);

            Assert.Equal(3, _viewModel.TimelineTrackCollection[0].TrackSegments.Count);
            Assert.Equal(_viewModel.TimelineSegments[0], _viewModel.TimelineTrackCollection[0].TrackSegments[0]);
            Assert.Equal(_viewModel.TimelineSegments[1], _viewModel.TimelineTrackCollection[0].TrackSegments[1]);
            Assert.Equal(_viewModel.TimelineSegments[2], _viewModel.TimelineTrackCollection[0].TrackSegments[2]);

            Assert.Equal(2, _viewModel.TimelineTrackCollection[1].TrackSegments.Count);
            Assert.Equal(_viewModel.TimelineSegments[3], _viewModel.TimelineTrackCollection[1].TrackSegments[0]);
            Assert.Equal(_viewModel.TimelineSegments[4], _viewModel.TimelineTrackCollection[1].TrackSegments[1]);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(3, _viewModel.TimelineTrackCollection.Count);

            Assert.Equal(10, _viewModel.TimelineSegments.Count);
            Assert.Equal(0, _viewModel.TimelineSegments[0].TrackNumber);
            Assert.Equal(0, _viewModel.TimelineSegments[1].TrackNumber);
            Assert.Equal(0, _viewModel.TimelineSegments[2].TrackNumber);
            Assert.Equal(0, _viewModel.TimelineSegments[3].TrackNumber);
            Assert.Equal(0, _viewModel.TimelineSegments[4].TrackNumber);
            Assert.Equal(1, _viewModel.TimelineSegments[5].TrackNumber);
            Assert.Equal(1, _viewModel.TimelineSegments[6].TrackNumber);
            Assert.Equal(1, _viewModel.TimelineSegments[7].TrackNumber);
            Assert.Equal(2, _viewModel.TimelineSegments[8].TrackNumber);
            Assert.Equal(2, _viewModel.TimelineSegments[9].TrackNumber);

            Assert.Equal(5, _viewModel.TimelineTrackCollection[0].TrackSegments.Count);
            Assert.Equal(_viewModel.TimelineSegments[0], _viewModel.TimelineTrackCollection[0].TrackSegments[0]);
            Assert.Equal(_viewModel.TimelineSegments[1], _viewModel.TimelineTrackCollection[0].TrackSegments[1]);
            Assert.Equal(_viewModel.TimelineSegments[2], _viewModel.TimelineTrackCollection[0].TrackSegments[2]);
            Assert.Equal(_viewModel.TimelineSegments[3], _viewModel.TimelineTrackCollection[0].TrackSegments[3]);
            Assert.Equal(_viewModel.TimelineSegments[4], _viewModel.TimelineTrackCollection[0].TrackSegments[4]);

            Assert.Equal(3, _viewModel.TimelineTrackCollection[1].TrackSegments.Count);
            Assert.Equal(_viewModel.TimelineSegments[5], _viewModel.TimelineTrackCollection[1].TrackSegments[0]);
            Assert.Equal(_viewModel.TimelineSegments[6], _viewModel.TimelineTrackCollection[1].TrackSegments[1]);
            Assert.Equal(_viewModel.TimelineSegments[7], _viewModel.TimelineTrackCollection[1].TrackSegments[2]);

            Assert.Equal(2, _viewModel.TimelineTrackCollection[2].TrackSegments.Count);
            Assert.Equal(_viewModel.TimelineSegments[8], _viewModel.TimelineTrackCollection[2].TrackSegments[0]);
            Assert.Equal(_viewModel.TimelineSegments[9], _viewModel.TimelineTrackCollection[2].TrackSegments[1]);
        }

        [Theory]
        [InlineData(0, 5, new int[] { 0 }, new int[] { 5, 10, 22 })]
        [InlineData(0, 10, new int[] { 0 }, new int[] { 10, 22 })]
        [InlineData(1, 5, new int[] { 0 }, new int[] { 5 })]
        public void SplitSelectedTrackSegmentCommandTest(int trackNumber, int frameNumber, int[] expectedToSplitKeyFrameNumbers, int[] expectedSplitKeyFrameNumbers)
        {
            SetupViewModel(useRealUndoService: true);
            IScriptVideoContext svc = _scriptVideoContextMock.Object;

            if (_viewModel.SelectedTrack.TrackNumber != trackNumber)
            {
                _viewModel.SelectedTrack = _viewModel.TimelineTrackCollection[trackNumber];
            }

            if (svc.FrameNumber != frameNumber)
            {
                svc.FrameNumber = frameNumber;
            }

            Assert.NotNull(_viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);
            SegmentViewModelBase segmentViewModelToSplit = _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment;
            int originalEndFrame = segmentViewModelToSplit.EndFrame;
            int[] originalKeyFrameNumbers = segmentViewModelToSplit.KeyFrameViewModels.Select(kfvm => kfvm.FrameNumber).ToArray();

            Assert.True(
                _viewModel.SplitSelectedTrackSegmentCommand.CanExecute()
            );
            _viewModel.SplitSelectedTrackSegmentCommand.Execute();

            SegmentViewModelBase splitSegmentViewModel;
            RunPreUndoTests();

            _undoRoot.Undo();
            RunPostUndoTests();

            _undoRoot.Redo();
            RunPreUndoTests();

            void RunPreUndoTests()
            {
                Assert.NotNull(_viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);
                Assert.NotSame(segmentViewModelToSplit, _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);

                Assert.False(
                    _viewModel.SplitSelectedTrackSegmentCommand.CanExecute()
                );

                splitSegmentViewModel = _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment;

                Assert.Equal(frameNumber - 1, segmentViewModelToSplit.EndFrame);
                Assert.Equal(expectedToSplitKeyFrameNumbers.Length, segmentViewModelToSplit.KeyFrameViewModels.Count);
                for (int i = 0; i < expectedToSplitKeyFrameNumbers.Length; i++)
                {
                    Assert.Equal(expectedToSplitKeyFrameNumbers[i], segmentViewModelToSplit.KeyFrameViewModels[i].FrameNumber);
                }

                Assert.Equal(frameNumber, splitSegmentViewModel.StartFrame);
                Assert.Equal(originalEndFrame, splitSegmentViewModel.EndFrame);

                Assert.Equal(expectedSplitKeyFrameNumbers.Length, splitSegmentViewModel.KeyFrameViewModels.Count);
                for (int i = 0; i < expectedSplitKeyFrameNumbers.Length; i++)
                {
                    Assert.Equal(expectedSplitKeyFrameNumbers[i], splitSegmentViewModel.KeyFrameViewModels[i].FrameNumber);
                }

                Assert.True(_undoRoot.CanUndo);
            }

            void RunPostUndoTests()
            {
                Assert.Same(segmentViewModelToSplit, _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);
                Assert.DoesNotContain(splitSegmentViewModel, _viewModel.TimelineSegments);

                Assert.True(
                    _viewModel.SplitSelectedTrackSegmentCommand.CanExecute()
                );

                Assert.Equal(originalEndFrame, segmentViewModelToSplit.EndFrame);

                Assert.Equal(originalKeyFrameNumbers.Length, segmentViewModelToSplit.KeyFrameViewModels.Count);
                for (int i = 0; i < originalKeyFrameNumbers.Length; i++)
                {
                    Assert.Equal(originalKeyFrameNumbers[i], segmentViewModelToSplit.KeyFrameViewModels[i].FrameNumber);
                }

                Assert.True(_undoRoot.CanRedo);
            }
        }

        [Theory]
        [InlineData(0, 0, 2, false, false)]
        [InlineData(0, 0, 3, true, false)]
        [InlineData(700, 0, 3, true, true)]
        public void JoinTrackSegmentLeftCommandTest(int frameNumber, int trackIndex, int trackSegmentIndex, bool expectedCanExecute, bool expectedSelectedSegment)
        {
            SetupViewModel(useRealUndoService: expectedCanExecute);

            IScriptVideoContext svc = _scriptVideoContextMock.Object;
            if (svc.FrameNumber != frameNumber)
            {
                svc.FrameNumber = frameNumber;
            }

            IVideoTimelineTrackViewModel timelineTrack = _viewModel.TimelineTrackCollection[trackIndex];
            SegmentViewModelBase segmentRightViewModel = timelineTrack.TrackSegments[trackSegmentIndex];

            Assert.Equal(expectedSelectedSegment, segmentRightViewModel == _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);

            Assert.Equal(expectedCanExecute, _viewModel.MergeTrackSegmentLeftCommand.CanExecute(segmentRightViewModel));

            int segmentRightKeyFrameCount = segmentRightViewModel.KeyFrameViewModels.Count;

            SegmentViewModelBase segmentLeftViewModel;
            int segmentLeftEndFrame;
            int segmentLeftKeyFrameCount;

            if (expectedCanExecute)
            {
                segmentLeftViewModel = timelineTrack.TrackSegments[trackSegmentIndex - 1];
                segmentLeftEndFrame = segmentLeftViewModel.EndFrame;
                segmentLeftKeyFrameCount = segmentLeftViewModel.KeyFrameViewModels.Count;

                _viewModel.MergeTrackSegmentLeftCommand.Execute(segmentRightViewModel);

                RunPreUndoTests();

                _undoRoot.Undo();
                RunPostUndoTests();

                _undoRoot.Redo();
                RunPreUndoTests();
            }

            void RunPreUndoTests()
            {
                Assert.Empty(segmentRightViewModel.KeyFrameViewModels);
                Assert.Empty(segmentRightViewModel.Model.KeyFrames);
                Assert.DoesNotContain(segmentRightViewModel, _viewModel.TimelineSegments);
                Assert.DoesNotContain(segmentRightViewModel, timelineTrack.TrackSegments);

                Assert.Equal(segmentLeftKeyFrameCount + segmentRightKeyFrameCount, segmentLeftViewModel.KeyFrameViewModels.Count);
                Assert.Equal(segmentLeftKeyFrameCount + segmentRightKeyFrameCount, segmentLeftViewModel.Model.KeyFrames.Count);
                Assert.Equal(segmentRightViewModel.EndFrame, segmentLeftViewModel.EndFrame);

                if (expectedSelectedSegment)
                {
                    Assert.False(segmentRightViewModel == _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);
                    Assert.True(segmentLeftViewModel == _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);
                }

                Assert.True(_undoRoot.CanUndo);
            }

            void RunPostUndoTests()
            {
                Assert.Contains(segmentRightViewModel, _viewModel.TimelineSegments);
                Assert.Contains(segmentRightViewModel, timelineTrack.TrackSegments);

                Assert.Equal(segmentLeftEndFrame, segmentLeftViewModel.EndFrame);
                Assert.Equal(segmentLeftKeyFrameCount, segmentLeftViewModel.KeyFrameViewModels.Count);
                Assert.Equal(segmentLeftKeyFrameCount, segmentLeftViewModel.Model.KeyFrames.Count);

                Assert.Equal(segmentRightKeyFrameCount, segmentRightViewModel.KeyFrameViewModels.Count);
                Assert.Equal(segmentRightKeyFrameCount, segmentRightViewModel.Model.KeyFrames.Count);

                if (expectedSelectedSegment)
                {
                    Assert.False(segmentLeftViewModel == _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);
                    Assert.True(segmentRightViewModel == _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);
                }

                Assert.True(_undoRoot.CanRedo);
            }
        }

        [Theory]
        [InlineData(0, 0, 4, false, false)]
        [InlineData(0, 0, 2, true, false)]
        [InlineData(700, 0, 2, true, true)]
        public void JoinTrackSegmentRightCommandTest(int frameNumber, int trackIndex, int trackSegmentIndex, bool expectedCanExecute, bool expectedRightSelectedSegment)
        {
            SetupViewModel(useRealUndoService: expectedCanExecute);

            IScriptVideoContext svc = _scriptVideoContextMock.Object;
            if (svc.FrameNumber != frameNumber)
            {
                svc.FrameNumber = frameNumber;
            }

            IVideoTimelineTrackViewModel timelineTrack = _viewModel.TimelineTrackCollection[trackIndex];
            SegmentViewModelBase segmentLeftViewModel = timelineTrack.TrackSegments[trackSegmentIndex];
            int segmentLeftEndFrame = segmentLeftViewModel.EndFrame;
            int segmentLeftKeyFrameCount = segmentLeftViewModel.KeyFrameViewModels.Count;

            SegmentViewModelBase segmentRightViewModel;
            int segmentRightKeyFrameCount;

            Assert.Equal(expectedCanExecute, _viewModel.MergeTrackSegmentRightCommand.CanExecute(segmentLeftViewModel));
            if (expectedCanExecute)
            {
                segmentRightViewModel = timelineTrack.TrackSegments[trackSegmentIndex + 1];
                Assert.Equal(expectedRightSelectedSegment, segmentRightViewModel == _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);
                segmentRightKeyFrameCount = segmentRightViewModel.KeyFrameViewModels.Count;

                _viewModel.MergeTrackSegmentRightCommand.Execute(segmentLeftViewModel);

                RunPreUndoTests();

                _undoRoot.Undo();
                RunPostUndoTests();

                _undoRoot.Redo();
                RunPreUndoTests();
            }

            void RunPreUndoTests()
            {
                Assert.Empty(segmentRightViewModel.KeyFrameViewModels);
                Assert.Empty(segmentRightViewModel.Model.KeyFrames);
                Assert.DoesNotContain(segmentRightViewModel, _viewModel.TimelineSegments);
                Assert.DoesNotContain(segmentRightViewModel, timelineTrack.TrackSegments);

                Assert.Equal(segmentLeftKeyFrameCount + segmentRightKeyFrameCount, segmentLeftViewModel.KeyFrameViewModels.Count);
                Assert.Equal(segmentLeftKeyFrameCount + segmentRightKeyFrameCount, segmentLeftViewModel.Model.KeyFrames.Count);
                Assert.Equal(segmentRightViewModel.EndFrame, segmentLeftViewModel.EndFrame);

                if (expectedRightSelectedSegment)
                {
                    Assert.False(segmentRightViewModel == _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);
                    Assert.True(segmentLeftViewModel == _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);
                }

                Assert.True(_undoRoot.CanUndo);
            }

            void RunPostUndoTests()
            {
                Assert.Contains(segmentRightViewModel, _viewModel.TimelineSegments);
                Assert.Contains(segmentRightViewModel, timelineTrack.TrackSegments);

                Assert.Equal(segmentLeftEndFrame, segmentLeftViewModel.EndFrame);
                Assert.Equal(segmentLeftKeyFrameCount, segmentLeftViewModel.KeyFrameViewModels.Count);
                Assert.Equal(segmentLeftKeyFrameCount, segmentLeftViewModel.Model.KeyFrames.Count);

                Assert.Equal(segmentRightKeyFrameCount, segmentRightViewModel.KeyFrameViewModels.Count);
                Assert.Equal(segmentRightKeyFrameCount, segmentRightViewModel.Model.KeyFrames.Count);

                if (expectedRightSelectedSegment)
                {
                    Assert.False(segmentLeftViewModel == _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);
                    Assert.True(segmentRightViewModel == _viewModel.TimelineSegmentProvidingViewModel.SelectedSegment);
                }

                Assert.True(_undoRoot.CanRedo);
            }
        }

        [Theory]
        [InlineData(5)]    // 'Expand' test
        [InlineData(20)]   // 'Contract' test
        public void ChangeTrackSegmentStartFrame_SingleKeyFrame_Test(int newStartFrame)
        {
            SetupViewModel(useRealUndoService: true);

            SegmentViewModelBase segmentToTest = _viewModel.TimelineSegments[8];

            int originalStartFrame = segmentToTest.StartFrame;
            Assert.True(_viewModel.CanChangeTrackSegmentStartFrame(segmentToTest, newStartFrame));

            _viewModel.ChangeTrackSegmentStartFrame(segmentToTest, newStartFrame);
            Assert.Equal(newStartFrame, segmentToTest.StartFrame);
            Assert.Single(segmentToTest.Model.KeyFrames);
            Assert.Single(segmentToTest.KeyFrameViewModels);
            Assert.Equal(newStartFrame, segmentToTest.KeyFrameViewModels[0].FrameNumber);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalStartFrame, segmentToTest.StartFrame);
            Assert.Single(segmentToTest.Model.KeyFrames);
            Assert.Single(segmentToTest.KeyFrameViewModels);
            Assert.Equal(originalStartFrame, segmentToTest.KeyFrameViewModels[0].FrameNumber);
        }

        [Fact]
        public void ChangeTrackSegmentStartFrame_LerpKeyFrame_Test()
        {
            SetupViewModel(useRealUndoService: true);

            SegmentViewModelBase segmentToTest = _viewModel.TimelineSegments[0];

            int newStartFrame = 5;
            int originalStartFrame = segmentToTest.StartFrame;
            Assert.True(_viewModel.CanChangeTrackSegmentStartFrame(segmentToTest, newStartFrame));

            _viewModel.ChangeTrackSegmentStartFrame(segmentToTest, newStartFrame);
            Assert.Equal(newStartFrame, segmentToTest.StartFrame);
            Assert.Equal(3, segmentToTest.Model.KeyFrames.Count);
            Assert.Equal(3, segmentToTest.KeyFrameViewModels.Count);
            Assert.Equal(newStartFrame, segmentToTest.KeyFrameViewModels[0].FrameNumber);
            Assert.Equal(10, segmentToTest.KeyFrameViewModels[1].FrameNumber);
            Assert.Equal(22, segmentToTest.KeyFrameViewModels[2].FrameNumber);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalStartFrame, segmentToTest.StartFrame);
            Assert.Equal(3, segmentToTest.Model.KeyFrames.Count);
            Assert.Equal(3, segmentToTest.KeyFrameViewModels.Count);
            Assert.Equal(originalStartFrame, segmentToTest.KeyFrameViewModels[0].FrameNumber);
            Assert.Equal(10, segmentToTest.KeyFrameViewModels[1].FrameNumber);
            Assert.Equal(22, segmentToTest.KeyFrameViewModels[2].FrameNumber);
        }

        [Fact]
        public void ChangeTrackSegmentStartFrame_CutKeyFrame_Test()
        {
            SetupViewModel(useRealUndoService: true);

            SegmentViewModelBase segmentToTest = _viewModel.TimelineSegments[0];

            int newStartFrame = 10;
            int originalStartFrame = segmentToTest.StartFrame;
            Assert.True(_viewModel.CanChangeTrackSegmentStartFrame(segmentToTest, newStartFrame));

            _viewModel.ChangeTrackSegmentStartFrame(segmentToTest, newStartFrame);
            Assert.Equal(newStartFrame, segmentToTest.StartFrame);
            Assert.Equal(2, segmentToTest.Model.KeyFrames.Count);
            Assert.Equal(2, segmentToTest.KeyFrameViewModels.Count);
            Assert.Equal(newStartFrame, segmentToTest.KeyFrameViewModels[0].FrameNumber);
            Assert.Equal(22, segmentToTest.KeyFrameViewModels[1].FrameNumber);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalStartFrame, segmentToTest.StartFrame);
            Assert.Equal(3, segmentToTest.Model.KeyFrames.Count);
            Assert.Equal(3, segmentToTest.KeyFrameViewModels.Count);
            Assert.Equal(originalStartFrame, segmentToTest.KeyFrameViewModels[0].FrameNumber);
            Assert.Equal(10, segmentToTest.KeyFrameViewModels[1].FrameNumber);
            Assert.Equal(22, segmentToTest.KeyFrameViewModels[2].FrameNumber);
        }

        [Theory]
        [InlineData(40)]
        [InlineData(20)]
        public void ChangeTrackSegmentEndFrame_SingleKeyFrame_Test(int newEndFrame)
        {
            SetupViewModel(useRealUndoService: true);

            SegmentViewModelBase segmentToTest = _viewModel.TimelineSegments[8];

            int originalEndFrame = segmentToTest.EndFrame;
            Assert.True(_viewModel.CanChangeTrackSegmentEndFrame(segmentToTest, newEndFrame));

            _viewModel.ChangeTrackSegmentEndFrame(segmentToTest, newEndFrame);
            Assert.Equal(newEndFrame, segmentToTest.EndFrame);
            Assert.Single(segmentToTest.Model.KeyFrames);
            Assert.Single(segmentToTest.KeyFrameViewModels);
            Assert.Equal(segmentToTest.StartFrame, segmentToTest.KeyFrameViewModels[0].FrameNumber);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalEndFrame, segmentToTest.EndFrame);
            Assert.Single(segmentToTest.Model.KeyFrames);
            Assert.Single(segmentToTest.KeyFrameViewModels);
            Assert.Equal(segmentToTest.StartFrame, segmentToTest.KeyFrameViewModels[0].FrameNumber);
        }

        [Fact]
        public void ChangeTrackSegmentEndFrameTest()
        {
            SetupViewModel(useRealUndoService: true);

            SegmentViewModelBase segmentToTest = _viewModel.TimelineSegments[0];

            int newEndFrame = 15;
            int originalEndFrame = segmentToTest.EndFrame;
            Assert.True(_viewModel.CanChangeTrackSegmentEndFrame(segmentToTest, newEndFrame));

            _viewModel.ChangeTrackSegmentEndFrame(segmentToTest, newEndFrame);
            Assert.Equal(newEndFrame, segmentToTest.EndFrame);
            Assert.Equal(2, segmentToTest.Model.KeyFrames.Count);
            Assert.Equal(2, segmentToTest.KeyFrameViewModels.Count);
            Assert.Equal(segmentToTest.StartFrame, segmentToTest.KeyFrameViewModels[0].FrameNumber);
            Assert.Equal(10, segmentToTest.KeyFrameViewModels[1].FrameNumber);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalEndFrame, segmentToTest.EndFrame);
            Assert.Equal(3, segmentToTest.Model.KeyFrames.Count);
            Assert.Equal(3, segmentToTest.KeyFrameViewModels.Count);
            Assert.Equal(segmentToTest.StartFrame, segmentToTest.KeyFrameViewModels[0].FrameNumber);
            Assert.Equal(10, segmentToTest.KeyFrameViewModels[1].FrameNumber);
            Assert.Equal(22, segmentToTest.KeyFrameViewModels[2].FrameNumber);
        }

        [Theory]
        [InlineData(0, 0, 0, false, false)]
        [InlineData(0, 10, 0, true, false)]
        [InlineData(0, 15, 10, true, true)]
        [InlineData(0, 122, 22, true, true)]
        [InlineData(0, 50, 22, true, true)]
        [InlineData(0, 300, 268, true, true)]
        [InlineData(0, 800, 789, true, true)]
        [InlineData(1, 117, 114, true, true)]
        [InlineData(1, 107, 33, true, true)]
        public void SeekPreviousKeyFrameCommandTest(int trackNumber, int preSeekFrameNumber, int expectedPostSeekFrameNumber, bool expectedPreSeekCanExecute, bool expectedPostSeekCanExecute)
        {
            SetupViewModel(useRealUndoService: false);
            IScriptVideoContext svc = _scriptVideoContextMock.Object;

            _viewModel.SelectedTrack = _viewModel.TimelineTrackCollection[trackNumber];
            svc.FrameNumber = preSeekFrameNumber;

            Assert.Equal(expectedPreSeekCanExecute, _viewModel.SeekPreviousKeyFrameInTrackCommand.CanExecute());
            if (expectedPreSeekCanExecute)
            {
                _viewModel.SeekPreviousKeyFrameInTrackCommand.Execute();
                Assert.Equal(expectedPostSeekFrameNumber, svc.FrameNumber);
                Assert.Equal(expectedPostSeekCanExecute, _viewModel.SeekPreviousKeyFrameInTrackCommand.CanExecute());
            }
        }

        [Fact]
        public void SeekNextKeyFrameCommandTest()
        {
            SetupViewModel(useRealUndoService: false);
            IScriptVideoContext svc = _scriptVideoContextMock.Object;

            Assert.NotNull(_viewModel.SelectedTrack);
            Assert.Equal(0, _viewModel.SelectedTrack.TrackNumber);

            Assert.True(_viewModel.SeekNextKeyFrameInTrackCommand.CanExecute());
            _viewModel.SeekNextKeyFrameInTrackCommand.Execute();
            Assert.Equal(10, svc.FrameNumber);

            svc.FrameNumber = 15;

            Assert.True(_viewModel.SeekNextKeyFrameInTrackCommand.CanExecute());
            _viewModel.SeekNextKeyFrameInTrackCommand.Execute();
            Assert.Equal(22, svc.FrameNumber);

            Assert.True(_viewModel.SeekNextKeyFrameInTrackCommand.CanExecute());
            _viewModel.SeekNextKeyFrameInTrackCommand.Execute();
            Assert.Equal(122, svc.FrameNumber);
            Assert.True(_viewModel.SeekNextKeyFrameInTrackCommand.CanExecute());

            svc.FrameNumber = 50;
            Assert.True(_viewModel.SeekNextKeyFrameInTrackCommand.CanExecute());
            _viewModel.SeekNextKeyFrameInTrackCommand.Execute();
            Assert.Equal(122, svc.FrameNumber);
            Assert.True(_viewModel.SeekNextKeyFrameInTrackCommand.CanExecute());
        }

        private void SetupViewModel(bool useRealUndoService = false)
        {
            _currentScriptVideoFrameNumber = 0;

            _scriptVideoServiceMock = new Mock<IScriptVideoService>();
            _scriptVideoServiceMock.SetupAdd(svs => svs.FrameChanged += It.IsAny<EventHandler<FrameChangedEventArgs>>());
            _scriptVideoServiceMock.SetupRemove(svs => svs.FrameChanged -= It.IsAny<EventHandler<FrameChangedEventArgs>>());

            _scriptVideoContextMock = new Mock<IScriptVideoContext>();
            _scriptVideoContextMock.Setup(svc => svc.HasVideo).Returns(true);

            _scriptVideoContextMock.SetupGet(svc => svc.FrameNumber).Returns(() => _currentScriptVideoFrameNumber);
            _scriptVideoContextMock.SetupSet(svc => svc.FrameNumber = It.IsAny<int>()).Callback<int>(value =>
            {
                int previousFrameNumber = _currentScriptVideoFrameNumber;
                _currentScriptVideoFrameNumber = value;
                _scriptVideoServiceMock.Raise(svs => svs.FrameChanged += null, new FrameChangedEventArgs(previousFrameNumber, _currentScriptVideoFrameNumber));
            });

            _scriptVideoContextMock.Setup(svc => svc.IsVideoPlaying).Returns(false);
            _scriptVideoContextMock.Setup(svc => svc.VideoFrameCount).Returns(400);
            _scriptVideoContextMock.Setup(svc => svc.SeekableVideoFrameCount).Returns(399);
            _scriptVideoContextMock.Setup(svc => svc.VideoFrameSize).Returns(new SizeI(640, 480));

            _scriptVideoServiceMock.Setup(svs => svs.GetContextReference()).Returns(_scriptVideoContextMock.Object);

            _timelineCommands = new TimelineCommands();
            _applicationCommands = new ApplicationCommands();

            if (useRealUndoService)
            {
                _undoService = UndoService.Current;
                _undoService.Clear();
                _undoChangeFactory = new ChangeFactory();
            }
            else
            {
                Mock<IUndoService> undoServiceMock;
                Mock<IChangeFactory> undoChangeFactoryMock;

                undoServiceMock = new Mock<IUndoService>();
                undoServiceMock.Setup(us => us[It.IsAny<object>()]).Returns((object root) =>
                {
                    if (_undoRoot?.Root != root)
                    {
                        _undoRoot = new UndoRoot(root);
                    }
                    return _undoRoot;
                });
                _undoService = undoServiceMock.Object;

                undoChangeFactoryMock = new Mock<IChangeFactory>();
                _undoChangeFactory = undoChangeFactoryMock.Object;
            }

            _clipboardServiceMock = new Mock<IClipboardService>();
            _dialogServiceMock = new Mock<IDialogService>();

            _timelineSegmentProvidingViewModel = new MockTimelineSegmentProvidingViewModel(GenerateTestSegmentModels(), _scriptVideoServiceMock.Object, _undoService, _undoChangeFactory, _clipboardServiceMock.Object, _applicationCommands);

            if (useRealUndoService)
            {
                _undoRoot = _undoService[_timelineSegmentProvidingViewModel];
            }

            _viewModel = new VideoTimelineViewModel(_scriptVideoServiceMock.Object, _undoService, _undoChangeFactory, _clipboardServiceMock.Object, _dialogServiceMock.Object, _timelineCommands)
            {
                TimelineSegmentProvidingViewModel = _timelineSegmentProvidingViewModel
            };
            _scriptVideoContextMock.Object.FrameNumber = 0;
        }

        public void Dispose()
        {
            _undoService?.Clear();
        }

        private static SegmentModelCollection GenerateTestSegmentModels()
        {
            return new SegmentModelCollection()
            {
                new MockSegmentModel(0, 22, 0,
                    new KeyFrameModelCollection()
                    {
                        new MockKeyFrameModel(0),
                        new MockKeyFrameModel(10),
                        new MockKeyFrameModel(22)
                    },
                    "Mock Segment 0"
                ),
                new MockSegmentModel(122, 268, 0,
                    new KeyFrameModelCollection()
                    {
                        new MockKeyFrameModel(122),
                        new MockKeyFrameModel(127),
                        new MockKeyFrameModel(139),
                        new MockKeyFrameModel(152),
                        new MockKeyFrameModel(167),
                        new MockKeyFrameModel(216),
                        new MockKeyFrameModel(250),
                        new MockKeyFrameModel(255),
                        new MockKeyFrameModel(268)
                    },
                    "Mock Segment 1"
                ),
                new MockSegmentModel(567, 696, 0,
                    new KeyFrameModelCollection()
                    {
                        new MockKeyFrameModel(567),
                        new MockKeyFrameModel(590),
                        new MockKeyFrameModel(624),
                        new MockKeyFrameModel(677),
                        new MockKeyFrameModel(696)
                    },
                    "Mock Segment 2"
                ),
                new MockSegmentModel(697, 722, 0,
                    new KeyFrameModelCollection()
                    {
                        new MockKeyFrameModel(697),
                        new MockKeyFrameModel(710)
                    },
                    "Mock Segment 3"
                ),
                new MockSegmentModel(723, 789, 0,
                    new KeyFrameModelCollection()
                    {
                        new MockKeyFrameModel(723),
                        new MockKeyFrameModel(742),
                        new MockKeyFrameModel(752),
                        new MockKeyFrameModel(767),
                        new MockKeyFrameModel(776),
                        new MockKeyFrameModel(789)
                    },
                    "Mock Segment 4"
                ),
                new MockSegmentModel(0, 22, 1,
                    new KeyFrameModelCollection()
                    {
                        new MockKeyFrameModel(0)
                    },
                    "Mock Segment 5"
                ),
                new MockSegmentModel(24, 33, 1,
                    new KeyFrameModelCollection()
                    {
                        new MockKeyFrameModel(24),
                        new MockKeyFrameModel(33)
                    },
                    "Mock Segment 6"
                ),
                new MockSegmentModel(107, 299, 1,
                    new KeyFrameModelCollection()
                    {
                        new MockKeyFrameModel(107),
                        new MockKeyFrameModel(114),
                        new MockKeyFrameModel(117),
                        new MockKeyFrameModel(122),
                        new MockKeyFrameModel(127),
                        new MockKeyFrameModel(139),
                        new MockKeyFrameModel(152),
                        new MockKeyFrameModel(167),
                        new MockKeyFrameModel(184),
                        new MockKeyFrameModel(202),
                        new MockKeyFrameModel(217),
                        new MockKeyFrameModel(232),
                        new MockKeyFrameModel(241),
                        new MockKeyFrameModel(255),
                        new MockKeyFrameModel(258),
                        new MockKeyFrameModel(262),
                        new MockKeyFrameModel(268),
                        new MockKeyFrameModel(277),
                        new MockKeyFrameModel(287)
                    },
                    "Mock Segment 7"
                ),
                new MockSegmentModel(10, 32, 2,
                    new KeyFrameModelCollection()
                    {
                        new MockKeyFrameModel(10)
                    },
                    "Mock Segment 8"
                ),
                new MockSegmentModel(100, 322, 2,
                    new KeyFrameModelCollection()
                    {
                        new MockKeyFrameModel(100),
                        new MockKeyFrameModel(120),
                        new MockKeyFrameModel(145),
                        new MockKeyFrameModel(160),
                        new MockKeyFrameModel(184),
                        new MockKeyFrameModel(195),
                        new MockKeyFrameModel(207),
                        new MockKeyFrameModel(215),
                        new MockKeyFrameModel(220),
                        new MockKeyFrameModel(266),
                        new MockKeyFrameModel(282),
                        new MockKeyFrameModel(296),
                        new MockKeyFrameModel(317)
                    },
                    "Mock Segment 9"
                )
            };
        }
    }
}