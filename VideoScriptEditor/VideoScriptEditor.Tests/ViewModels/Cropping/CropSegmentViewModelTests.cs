using MonitoredUndo;
using Moq;
using System;
using System.Linq;
using System.Windows;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Extensions;
using VideoScriptEditor.Models.Cropping;
using VideoScriptEditor.Services;
using VideoScriptEditor.Services.ScriptVideo;
using VideoScriptEditor.ViewModels.Timeline;
using Xunit;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.ViewModels.Cropping.Tests
{
    public class CropSegmentViewModelTests : IDisposable
    {
        private int DoubleEqualityPrecision => MathExtensions.FloatingPointPrecision;

        private Mock<IScriptVideoContext> _scriptVideoContextMock;
        private IUndoService _undoService;
        private IChangeFactory _undoChangeFactory;
        private UndoRoot _undoRoot;
        private Mock<ISupportsUndo> _rootUndoObjectMock;
        private Mock<IClipboardService> _clipboardServiceMock;
        private int _currentScriptVideoFrameNumber;

        private CropSegmentViewModel _viewModel;

        public CropSegmentViewModelTests()
        {
            _undoService = UndoService.Current;
            _undoService.Clear();
            _undoChangeFactory = new ChangeFactory();
        }

        public void Dispose()
        {
            UndoService.Current.Clear();
        }

        private void SetupViewModel(CropSegmentModel testSegmentModel)
        {
            _scriptVideoContextMock = new Mock<IScriptVideoContext>();
            _scriptVideoContextMock.Setup(svc => svc.HasVideo).Returns(true);

            _scriptVideoContextMock.SetupGet(svc => svc.FrameNumber).Returns(() => _currentScriptVideoFrameNumber);
            _scriptVideoContextMock.SetupSet(svc => svc.FrameNumber = It.IsAny<int>()).Callback<int>(value =>
            {
                _currentScriptVideoFrameNumber = value;
                _viewModel.ActiveKeyFrame = _viewModel.KeyFrameViewModels.FirstOrDefault(kf => kf.FrameNumber == value);
                if (_viewModel.ActiveKeyFrame == null)
                {
                    KeyFrameViewModelBase previousKeyFrame = _viewModel.KeyFrameViewModels.LastOrDefault(kf => kf.FrameNumber < value);
                    KeyFrameViewModelBase nextKeyFrame = _viewModel.KeyFrameViewModels.FirstOrDefault(kf => kf.FrameNumber > value)
                                                         ?? previousKeyFrame;
                    if (previousKeyFrame != null && nextKeyFrame != null)
                    {
                        int frameRange = nextKeyFrame.FrameNumber - previousKeyFrame.FrameNumber;
                        double lerpAmount = (frameRange > 0) ? (double)(value - previousKeyFrame.FrameNumber) / frameRange : 0d;
                        _viewModel.Lerp(previousKeyFrame, nextKeyFrame, lerpAmount);
                    }
                }
            });

            _scriptVideoContextMock.Setup(svc => svc.IsVideoPlaying).Returns(false);
            _scriptVideoContextMock.Setup(svc => svc.VideoFrameCount).Returns(241);
            _scriptVideoContextMock.Setup(svc => svc.SeekableVideoFrameCount).Returns(240);
            _scriptVideoContextMock.Setup(svc => svc.VideoFrameSize).Returns(new SizeI(628, 472));

            _rootUndoObjectMock = new Mock<ISupportsUndo>();
            _rootUndoObjectMock.Setup(ruo => ruo.GetUndoRoot()).Returns(_rootUndoObjectMock.Object);

            _undoRoot = _undoService[_rootUndoObjectMock.Object];

            _clipboardServiceMock = new Mock<IClipboardService>();

            _viewModel = new CropSegmentViewModel(testSegmentModel, _scriptVideoContextMock.Object, _rootUndoObjectMock.Object, _undoService, _undoChangeFactory, _clipboardServiceMock.Object);
            _scriptVideoContextMock.Object.FrameNumber = 0;
        }

        private CropSegmentModel GenerateZeroAngleTestSegmentModel()
        {
            return new CropSegmentModel(0, 10, 0,
                new KeyFrameModelCollection()
                {
                    new CropKeyFrameModel(0, 155d, 56d, 388d, 309d, 0d),
                    new CropKeyFrameModel(10, 100d, 181d, 351d, 230d, 0d)
                },
                "Zero Angle Test"
            );
        }

        private CropSegmentModel GenerateAngledTestSegmentModel()
        {
            return new CropSegmentModel(0, 20, 0,
                new KeyFrameModelCollection()
                {
                    new CropKeyFrameModel(0, 155d, 56d, 388d, 309d, 15d),
                    new CropKeyFrameModel(10, 74d, 70d, 286d, 309d, -24d),
                    new CropKeyFrameModel(20, 155d, 56d, 388d, 309d, 0d)
                },
                "Angled Test"
            );
        }

        [Fact]
        public void ZeroAngledKeyFrameTest()
        {
            SetupViewModel(GenerateZeroAngleTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel keyFrame = (CropKeyFrameViewModel)_viewModel.KeyFrameViewModels[0];
            Assert.Equal(keyFrame, _viewModel.ActiveKeyFrame);

            Assert.Equal(keyFrame.Left, _viewModel.DataLeft);
            Assert.Equal(keyFrame.Top, _viewModel.DataTop);

            Assert.Equal(keyFrame.Angle, _viewModel.Angle);
            Assert.Equal(new Point(keyFrame.Left, keyFrame.Top), _viewModel.VisualTopLeft);
            Assert.Equal(new Point(keyFrame.Left + keyFrame.Width, keyFrame.Top), _viewModel.VisualTopRight);
            Assert.Equal(new Point(keyFrame.Left, keyFrame.Top + keyFrame.Height), _viewModel.VisualBottomLeft);
            Assert.Equal(new Point(keyFrame.Left + keyFrame.Width, keyFrame.Top + keyFrame.Height), _viewModel.VisualBottomRight);
            Assert.Equal(keyFrame.Width, _viewModel.Width);
            Assert.Equal(keyFrame.Height, _viewModel.Height);
            Assert.Equal(new Point(349d, 210.5), _viewModel.Center);
        }

        [Fact]
        public void SetVisualTopLeftTest()
        {
            SetupViewModel(GenerateZeroAngleTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[0], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel activeKeyFrame = (CropKeyFrameViewModel)_viewModel.ActiveKeyFrame;

            Vector visualTopLeftSetVector = new Vector(50d, 50d);

            Point originalVisualTopLeft = _viewModel.VisualTopLeft;
            Point originalVisualTopRight = _viewModel.VisualTopRight;
            Point originalVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point originalVisualBottomRight = _viewModel.VisualBottomRight;
            Point originalCenter = _viewModel.Center;
            double originalWidth = _viewModel.Width;
            double originalHeight = _viewModel.Height;
            double originalAngle = _viewModel.Angle;
            double originalDataLeft = _viewModel.DataLeft;
            double originalDataTop = _viewModel.DataTop;

            Point newVisualTopLeft = originalVisualTopLeft + visualTopLeftSetVector;
            _viewModel.VisualTopLeft = newVisualTopLeft;

            Assert.Equal(newVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(new Point(originalVisualTopRight.X, originalVisualTopRight.Y + visualTopLeftSetVector.Y), _viewModel.VisualTopRight);
            Assert.Equal(new Point(originalVisualBottomLeft.X + visualTopLeftSetVector.X, originalVisualBottomLeft.Y), _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalWidth - visualTopLeftSetVector.X, _viewModel.Width);
            Assert.Equal(originalHeight - visualTopLeftSetVector.Y, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);
            Assert.Equal(originalCenter + (visualTopLeftSetVector / 2d), _viewModel.Center);

            Assert.Equal(newVisualTopLeft.X, _viewModel.DataLeft);
            Assert.Equal(newVisualTopLeft.Y, _viewModel.DataTop);

            Assert.Equal(newVisualTopLeft.X, activeKeyFrame.Left);
            Assert.Equal(newVisualTopLeft.Y, activeKeyFrame.Top);
            Assert.Equal(originalWidth - visualTopLeftSetVector.X, activeKeyFrame.Width);
            Assert.Equal(originalHeight - visualTopLeftSetVector.Y, activeKeyFrame.Height);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);
            Assert.Equal(originalAngle, activeKeyFrame.Angle);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalCenter, _viewModel.Center);
        }

        [Fact]
        public void SetVisualTopRightTest()
        {
            SetupViewModel(GenerateZeroAngleTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[0], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel activeKeyFrame = (CropKeyFrameViewModel)_viewModel.ActiveKeyFrame;

            Vector visualTopRightSetVector = new Vector(50d, 50d);

            Point originalVisualTopLeft = _viewModel.VisualTopLeft;
            Point originalVisualTopRight = _viewModel.VisualTopRight;
            Point originalVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point originalVisualBottomRight = _viewModel.VisualBottomRight;
            Point originalCenter = _viewModel.Center;
            double originalWidth = _viewModel.Width;
            double originalHeight = _viewModel.Height;
            double originalAngle = _viewModel.Angle;
            double originalDataLeft = _viewModel.DataLeft;
            double originalDataTop = _viewModel.DataTop;

            Point newVisualTopRight = originalVisualTopRight + visualTopRightSetVector;
            _viewModel.VisualTopRight = newVisualTopRight;

            Assert.Equal(newVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(new Point(originalVisualTopLeft.X, originalVisualTopLeft.Y + visualTopRightSetVector.Y), _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(new Point(originalVisualBottomRight.X + visualTopRightSetVector.X, originalVisualBottomRight.Y), _viewModel.VisualBottomRight);
            Assert.Equal(originalWidth + visualTopRightSetVector.X, _viewModel.Width);
            Assert.Equal(originalHeight - visualTopRightSetVector.Y, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);
            Assert.Equal(originalCenter + (visualTopRightSetVector / 2d), _viewModel.Center);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop + visualTopRightSetVector.Y, _viewModel.DataTop);

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop + visualTopRightSetVector.Y, activeKeyFrame.Top);
            Assert.Equal(originalWidth + visualTopRightSetVector.X, activeKeyFrame.Width);
            Assert.Equal(originalHeight - visualTopRightSetVector.Y, activeKeyFrame.Height);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);
            Assert.Equal(originalAngle, activeKeyFrame.Angle);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalCenter, _viewModel.Center);
        }

        [Fact]
        public void SetVisualBottomLeftTest()
        {
            SetupViewModel(GenerateZeroAngleTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[0], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel activeKeyFrame = (CropKeyFrameViewModel)_viewModel.ActiveKeyFrame;

            Vector visualBottomLeftSetVector = new Vector(50d, 50d);

            Point originalVisualTopLeft = _viewModel.VisualTopLeft;
            Point originalVisualTopRight = _viewModel.VisualTopRight;
            Point originalVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point originalVisualBottomRight = _viewModel.VisualBottomRight;
            Point originalCenter = _viewModel.Center;
            double originalWidth = _viewModel.Width;
            double originalHeight = _viewModel.Height;
            double originalAngle = _viewModel.Angle;
            double originalDataLeft = _viewModel.DataLeft;
            double originalDataTop = _viewModel.DataTop;

            Point newVisualBottomLeft = originalVisualBottomLeft + visualBottomLeftSetVector;
            _viewModel.VisualBottomLeft = newVisualBottomLeft;

            Assert.Equal(newVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(new Point(originalVisualTopLeft.X + visualBottomLeftSetVector.X, originalVisualTopLeft.Y), _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(new Point(originalVisualBottomRight.X, originalVisualBottomRight.Y + visualBottomLeftSetVector.Y), _viewModel.VisualBottomRight);
            Assert.Equal(originalWidth - visualBottomLeftSetVector.X, _viewModel.Width);
            Assert.Equal(originalHeight + visualBottomLeftSetVector.Y, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);
            Assert.Equal(originalCenter + (visualBottomLeftSetVector / 2d), _viewModel.Center);

            Assert.Equal(originalDataLeft + visualBottomLeftSetVector.X, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);

            Assert.Equal(originalDataLeft + visualBottomLeftSetVector.X, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth - visualBottomLeftSetVector.X, activeKeyFrame.Width);
            Assert.Equal(originalHeight + visualBottomLeftSetVector.Y, activeKeyFrame.Height);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);
            Assert.Equal(originalAngle, activeKeyFrame.Angle);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalCenter, _viewModel.Center);
        }

        [Fact]
        public void SetVisualBottomRightTest()
        {
            SetupViewModel(GenerateZeroAngleTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[0], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel activeKeyFrame = (CropKeyFrameViewModel)_viewModel.ActiveKeyFrame;

            Vector visualBottomRightSetVector = new Vector(50d, 50d);

            Point originalVisualTopLeft = _viewModel.VisualTopLeft;
            Point originalVisualTopRight = _viewModel.VisualTopRight;
            Point originalVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point originalVisualBottomRight = _viewModel.VisualBottomRight;
            Point originalCenter = _viewModel.Center;
            double originalWidth = _viewModel.Width;
            double originalHeight = _viewModel.Height;
            double originalAngle = _viewModel.Angle;
            double originalDataLeft = _viewModel.DataLeft;
            double originalDataTop = _viewModel.DataTop;

            Point newVisualBottomRight = originalVisualBottomRight + visualBottomRightSetVector;
            _viewModel.VisualBottomRight = newVisualBottomRight;

            Assert.Equal(newVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(new Point(originalVisualTopRight.X + visualBottomRightSetVector.X, originalVisualTopRight.Y), _viewModel.VisualTopRight);
            Assert.Equal(new Point(originalVisualBottomLeft.X, originalVisualBottomLeft.Y + visualBottomRightSetVector.Y), _viewModel.VisualBottomLeft);
            Assert.Equal(originalWidth + visualBottomRightSetVector.X, _viewModel.Width);
            Assert.Equal(originalHeight + visualBottomRightSetVector.Y, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);
            Assert.Equal(originalCenter + (visualBottomRightSetVector / 2d), _viewModel.Center);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth + visualBottomRightSetVector.X, activeKeyFrame.Width);
            Assert.Equal(originalHeight + visualBottomRightSetVector.Y, activeKeyFrame.Height);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);
            Assert.Equal(originalAngle, activeKeyFrame.Angle);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalCenter, _viewModel.Center);
        }

        [Fact]
        public void SetWidthTest()
        {
            SetupViewModel(GenerateZeroAngleTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[0], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel activeKeyFrame = (CropKeyFrameViewModel)_viewModel.ActiveKeyFrame;

            double widthSetDiff = 50d;

            Point originalVisualTopLeft = _viewModel.VisualTopLeft;
            Point originalVisualTopRight = _viewModel.VisualTopRight;
            Point originalVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point originalVisualBottomRight = _viewModel.VisualBottomRight;
            Point originalCenter = _viewModel.Center;
            double originalWidth = _viewModel.Width;
            double originalHeight = _viewModel.Height;
            double originalAngle = _viewModel.Angle;
            double originalDataLeft = _viewModel.DataLeft;
            double originalDataTop = _viewModel.DataTop;

            double newWidth = _viewModel.Width + widthSetDiff;
            _viewModel.Width = newWidth;

            Assert.Equal(newWidth, _viewModel.Width);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(new Point(originalVisualTopRight.X + widthSetDiff, originalVisualTopRight.Y), _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(new Point(originalVisualBottomRight.X + widthSetDiff, originalVisualBottomRight.Y), _viewModel.VisualBottomRight);
            Assert.Equal(newWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);
            Assert.Equal(new Point(originalCenter.X + (widthSetDiff / 2d), originalCenter.Y), _viewModel.Center);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(newWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);
            Assert.Equal(originalAngle, activeKeyFrame.Angle);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalCenter, _viewModel.Center);
        }

        [Fact]
        public void SetHeightTest()
        {
            SetupViewModel(GenerateZeroAngleTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[0], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel activeKeyFrame = (CropKeyFrameViewModel)_viewModel.ActiveKeyFrame;

            double heightSetDiff = 50d;

            Point originalVisualTopLeft = _viewModel.VisualTopLeft;
            Point originalVisualTopRight = _viewModel.VisualTopRight;
            Point originalVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point originalVisualBottomRight = _viewModel.VisualBottomRight;
            Point originalCenter = _viewModel.Center;
            double originalWidth = _viewModel.Width;
            double originalHeight = _viewModel.Height;
            double originalAngle = _viewModel.Angle;
            double originalDataLeft = _viewModel.DataLeft;
            double originalDataTop = _viewModel.DataTop;

            double newHeight = _viewModel.Height + heightSetDiff;
            _viewModel.Height = newHeight;

            Assert.Equal(newHeight, _viewModel.Height);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(new Point(originalVisualBottomLeft.X, originalVisualBottomLeft.Y + heightSetDiff), _viewModel.VisualBottomLeft);
            Assert.Equal(new Point(originalVisualBottomRight.X, originalVisualBottomRight.Y + heightSetDiff), _viewModel.VisualBottomRight);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(newHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);
            Assert.Equal(new Point(originalCenter.X, originalCenter.Y + (heightSetDiff / 2d)), _viewModel.Center);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(newHeight, activeKeyFrame.Height);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);
            Assert.Equal(originalAngle, activeKeyFrame.Angle);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalCenter, _viewModel.Center);
        }

        [Fact]
        public void SetCenterTest()
        {
            SetupViewModel(GenerateZeroAngleTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[0], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel activeKeyFrame = (CropKeyFrameViewModel)_viewModel.ActiveKeyFrame;

            Vector centerSetVector = new Vector(50d, 50d);

            Point originalVisualTopLeft = _viewModel.VisualTopLeft;
            Point originalVisualTopRight = _viewModel.VisualTopRight;
            Point originalVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point originalVisualBottomRight = _viewModel.VisualBottomRight;
            Point originalCenter = _viewModel.Center;
            double originalWidth = _viewModel.Width;
            double originalHeight = _viewModel.Height;
            double originalAngle = _viewModel.Angle;
            double originalDataLeft = _viewModel.DataLeft;
            double originalDataTop = _viewModel.DataTop;

            Point newCenter = originalCenter + centerSetVector;
            _viewModel.Center = newCenter;

            Assert.Equal(newCenter, _viewModel.Center);
            Assert.Equal(originalVisualTopLeft + centerSetVector, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight + centerSetVector, _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft + centerSetVector, _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight + centerSetVector, _viewModel.VisualBottomRight);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalDataLeft + centerSetVector.X, _viewModel.DataLeft);
            Assert.Equal(originalDataTop + centerSetVector.Y, _viewModel.DataTop);

            Assert.Equal(originalDataLeft + centerSetVector.X, activeKeyFrame.Left);
            Assert.Equal(originalDataTop + centerSetVector.Y, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);
            Assert.Equal(originalAngle, activeKeyFrame.Angle);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalCenter, _viewModel.Center);
        }

        [Fact]
        public void SetAngleTest()
        {
            SetupViewModel(GenerateZeroAngleTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[0], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel activeKeyFrame = (CropKeyFrameViewModel)_viewModel.ActiveKeyFrame;

            Point originalVisualTopLeft = _viewModel.VisualTopLeft;
            Point originalVisualTopRight = _viewModel.VisualTopRight;
            Point originalVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point originalVisualBottomRight = _viewModel.VisualBottomRight;
            Point originalCenter = _viewModel.Center;
            double originalWidth = _viewModel.Width;
            double originalHeight = _viewModel.Height;
            double originalAngle = _viewModel.Angle;
            double originalDataLeft = _viewModel.DataLeft;
            double originalDataTop = _viewModel.DataTop;

            double newAngle = 15d;
            _viewModel.Angle = newAngle;

            Assert.Equal(newAngle, _viewModel.Angle);

            Point newVisualTopLeft = _viewModel.VisualTopLeft;
            Point newVisualTopRight = _viewModel.VisualTopRight;
            Point newVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point newVisualBottomRight = _viewModel.VisualBottomRight;

            Assert.Equal(201.5979321682602, newVisualTopLeft.X);
            Assert.Equal(11.053565088449915, newVisualTopLeft.Y);

            Assert.Equal(576.37715276841868, newVisualTopRight.X);
            Assert.Equal(111.47535458822797, newVisualTopRight.Y);

            Assert.Equal(121.6228472315813, newVisualBottomLeft.X);
            Assert.Equal(309.524645411772, newVisualBottomLeft.Y);

            Assert.Equal(496.40206783173977, newVisualBottomRight.X);
            Assert.Equal(409.94643491155006, newVisualBottomRight.Y);

            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalCenter, _viewModel.Center);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);
            Assert.Equal(originalAngle, activeKeyFrame.Angle);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalCenter, _viewModel.Center);
        }

        [Fact]
        public void AngledKeyFrameTest()
        {
            SetupViewModel(GenerateAngledTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel keyFrame = (CropKeyFrameViewModel)_viewModel.KeyFrameViewModels[0];
            Assert.Equal(keyFrame, _viewModel.ActiveKeyFrame);

            Assert.Equal(keyFrame.Left, _viewModel.DataLeft);
            Assert.Equal(keyFrame.Top, _viewModel.DataTop);

            Assert.Equal(keyFrame.Angle, _viewModel.Angle);

            Point visualTopLeft = _viewModel.VisualTopLeft;
            Point visualTopRight = _viewModel.VisualTopRight;
            Point visualBottomLeft = _viewModel.VisualBottomLeft;
            Point visualBottomRight = _viewModel.VisualBottomRight;

            Assert.Equal(201.5979321682602, visualTopLeft.X);
            Assert.Equal(11.053565088449915, visualTopLeft.Y);

            Assert.Equal(576.37715276841868, visualTopRight.X);
            Assert.Equal(111.47535458822797, visualTopRight.Y);

            Assert.Equal(121.6228472315813, visualBottomLeft.X);
            Assert.Equal(309.524645411772, visualBottomLeft.Y);

            Assert.Equal(496.40206783173977, visualBottomRight.X);
            Assert.Equal(409.94643491155006, visualBottomRight.Y);

            Assert.Equal(keyFrame.Width, _viewModel.Width);
            Assert.Equal(keyFrame.Height, _viewModel.Height);
            Assert.Equal(new Point(349d, 210.5), _viewModel.Center);
        }

        [Fact]
        public void SetVisualTopLeftAngledTest()
        {
            SetupViewModel(GenerateAngledTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[0], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel activeKeyFrame = (CropKeyFrameViewModel)_viewModel.ActiveKeyFrame;

            Point originalVisualTopLeft = _viewModel.VisualTopLeft;
            Point originalVisualTopRight = _viewModel.VisualTopRight;
            Point originalVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point originalVisualBottomRight = _viewModel.VisualBottomRight;
            Point originalCenter = _viewModel.Center;
            double originalWidth = _viewModel.Width;
            double originalHeight = _viewModel.Height;
            double originalAngle = _viewModel.Angle;
            double originalDataLeft = _viewModel.DataLeft;
            double originalDataTop = _viewModel.DataTop;

            double expectedWidth = 376.93266498852239;
            double expectedHeight = 300.18606567384938;

            Point newVisualTopLeft = new Point(210.006942818149, 22.4316089647949);
            _viewModel.VisualTopLeft = newVisualTopLeft;

            Point visualTopLeft = _viewModel.VisualTopLeft;
            Assert.Equal(newVisualTopLeft.X, visualTopLeft.X, DoubleEqualityPrecision);
            Assert.Equal(newVisualTopLeft.Y, visualTopLeft.Y, DoubleEqualityPrecision);

            Point newVisualTopRight = _viewModel.VisualTopRight;
            Point newVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point newVisualBottomRight = _viewModel.VisualBottomRight;
            Point newCenter = _viewModel.Center;

            Assert.Equal(574.09593870252809, newVisualTopRight.X, DoubleEqualityPrecision);
            Assert.Equal(119.98896138507263, newVisualTopRight.Y, DoubleEqualityPrecision);

            Assert.Equal(132.31307194736075, newVisualBottomLeft.X, DoubleEqualityPrecision);
            Assert.Equal(312.38908249127235, newVisualBottomLeft.Y, DoubleEqualityPrecision);

            Assert.Equal(originalVisualBottomRight.X, newVisualBottomRight.X, DoubleEqualityPrecision);
            Assert.Equal(originalVisualBottomRight.Y, newVisualBottomRight.Y, DoubleEqualityPrecision);

            Assert.Equal(expectedWidth, _viewModel.Width, DoubleEqualityPrecision);
            Assert.Equal(expectedHeight, _viewModel.Height, DoubleEqualityPrecision);
            Assert.Equal(originalAngle, _viewModel.Angle, DoubleEqualityPrecision);

            Assert.Equal(353.20450532494442, newCenter.X, DoubleEqualityPrecision);
            Assert.Equal(216.18902193817249, newCenter.Y, DoubleEqualityPrecision);

            Assert.Equal(164.7381728306832, _viewModel.DataLeft);
            Assert.Equal(66.09598910124781, _viewModel.DataTop);

            Assert.Equal(164.7381728306832, activeKeyFrame.Left);
            Assert.Equal(66.09598910124781, activeKeyFrame.Top);
            Assert.Equal(expectedWidth, activeKeyFrame.Width, DoubleEqualityPrecision);
            Assert.Equal(expectedHeight, activeKeyFrame.Height, DoubleEqualityPrecision);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);
            Assert.Equal(originalAngle, activeKeyFrame.Angle);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalCenter, _viewModel.Center);
        }

        [Fact]
        public void SetVisualTopRightAngledTest()
        {
            SetupViewModel(GenerateAngledTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[0], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel activeKeyFrame = (CropKeyFrameViewModel)_viewModel.ActiveKeyFrame;

            Point originalVisualTopLeft = _viewModel.VisualTopLeft;
            Point originalVisualTopRight = _viewModel.VisualTopRight;
            Point originalVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point originalVisualBottomRight = _viewModel.VisualBottomRight;
            Point originalCenter = _viewModel.Center;
            double originalWidth = _viewModel.Width;
            double originalHeight = _viewModel.Height;
            double originalAngle = _viewModel.Angle;
            double originalDataLeft = _viewModel.DataLeft;
            double originalDataTop = _viewModel.DataTop;

            double expectedWidth = 373.42189966476144;
            double expectedHeight = 297.39012112477133;

            Point newVisualTopRight = new Point(559.29093139213592, 118.91654642572408);
            _viewModel.VisualTopRight = newVisualTopRight;

            Point visualTopRight = _viewModel.VisualTopRight;
            Assert.Equal(newVisualTopRight.X, visualTopRight.X, DoubleEqualityPrecision);
            Assert.Equal(newVisualTopRight.Y, visualTopRight.Y, DoubleEqualityPrecision);

            Point newVisualTopLeft = _viewModel.VisualTopLeft;
            Point newVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point newVisualBottomRight = _viewModel.VisualBottomRight;
            Point newCenter = _viewModel.Center;

            Assert.Equal(198.59307440401761, newVisualTopLeft.X, DoubleEqualityPrecision);
            Assert.Equal(22.267846934121195, newVisualTopLeft.Y, DoubleEqualityPrecision);

            Assert.Equal(originalVisualBottomLeft.X, newVisualBottomLeft.X, DoubleEqualityPrecision);
            Assert.Equal(originalVisualBottomLeft.Y, newVisualBottomLeft.Y, DoubleEqualityPrecision);

            Assert.Equal(482.32070421969962, newVisualBottomRight.X, DoubleEqualityPrecision);
            Assert.Equal(406.17334490337487, newVisualBottomRight.Y, DoubleEqualityPrecision);

            Assert.Equal(expectedWidth, _viewModel.Width, DoubleEqualityPrecision);
            Assert.Equal(expectedHeight, _viewModel.Height, DoubleEqualityPrecision);
            Assert.Equal(originalAngle, _viewModel.Angle, DoubleEqualityPrecision);

            Assert.Equal(340.45688931185862, newCenter.X, DoubleEqualityPrecision);
            Assert.Equal(214.22059591874802, newCenter.Y, DoubleEqualityPrecision);

            Assert.Equal(153.74593947947787, _viewModel.DataLeft);
            Assert.Equal(65.5255353563624, _viewModel.DataTop);

            Assert.Equal(153.74593947947787, activeKeyFrame.Left);
            Assert.Equal(65.5255353563624, activeKeyFrame.Top);
            Assert.Equal(expectedWidth, activeKeyFrame.Width, DoubleEqualityPrecision);
            Assert.Equal(expectedHeight, activeKeyFrame.Height, DoubleEqualityPrecision);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);
            Assert.Equal(originalAngle, activeKeyFrame.Angle);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalCenter, _viewModel.Center);
        }

        [Fact]
        public void SetVisualBottomLeftAngledTest()
        {
            SetupViewModel(GenerateAngledTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[0], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel activeKeyFrame = (CropKeyFrameViewModel)_viewModel.ActiveKeyFrame;

            Point originalVisualTopLeft = _viewModel.VisualTopLeft;
            Point originalVisualTopRight = _viewModel.VisualTopRight;
            Point originalVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point originalVisualBottomRight = _viewModel.VisualBottomRight;
            Point originalCenter = _viewModel.Center;
            double originalWidth = _viewModel.Width;
            double originalHeight = _viewModel.Height;
            double originalAngle = _viewModel.Angle;
            double originalDataLeft = _viewModel.DataLeft;
            double originalDataTop = _viewModel.DataTop;

            double expectedWidth = 402.97541895670867;
            double expectedHeight = 320.92630014851289;

            Point newVisualBottomLeft = new Point(104.07094968575326, 317.16864310291021);
            _viewModel.VisualBottomLeft = newVisualBottomLeft;

            Point visualBottomLeft = _viewModel.VisualBottomLeft;
            Assert.Equal(newVisualBottomLeft.X, visualBottomLeft.X, DoubleEqualityPrecision);
            Assert.Equal(newVisualBottomLeft.Y, visualBottomLeft.Y, DoubleEqualityPrecision);

            Point newVisualTopLeft = _viewModel.VisualTopLeft;
            Point newVisualTopRight = _viewModel.VisualTopRight;
            Point newVisualBottomRight = _viewModel.VisualBottomRight;
            Point newCenter = _viewModel.Center;

            Assert.Equal(187.13278823847634, newVisualTopLeft.X, DoubleEqualityPrecision);
            Assert.Equal(7.1776414540643714, newVisualTopLeft.Y, DoubleEqualityPrecision);

            Assert.Equal(originalVisualTopRight.X, newVisualTopRight.X, DoubleEqualityPrecision);
            Assert.Equal(originalVisualTopRight.Y, newVisualTopRight.Y, DoubleEqualityPrecision);

            Assert.Equal(493.31531421569554, newVisualBottomRight.X, DoubleEqualityPrecision);
            Assert.Equal(421.4663562370738, newVisualBottomRight.Y, DoubleEqualityPrecision);

            Assert.Equal(expectedWidth, _viewModel.Width, DoubleEqualityPrecision);
            Assert.Equal(expectedHeight, _viewModel.Height, DoubleEqualityPrecision);
            Assert.Equal(originalAngle, _viewModel.Angle, DoubleEqualityPrecision);

            Assert.Equal(340.22405122708597, newCenter.X, DoubleEqualityPrecision);
            Assert.Equal(214.32199884556908, newCenter.Y, DoubleEqualityPrecision);

            Assert.Equal(138.73634174873163, _viewModel.DataLeft);
            Assert.Equal(53.858848771312637, _viewModel.DataTop);

            Assert.Equal(138.73634174873163, activeKeyFrame.Left);
            Assert.Equal(53.858848771312637, activeKeyFrame.Top);
            Assert.Equal(expectedWidth, activeKeyFrame.Width, DoubleEqualityPrecision);
            Assert.Equal(expectedHeight, activeKeyFrame.Height, DoubleEqualityPrecision);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);
            Assert.Equal(originalAngle, activeKeyFrame.Angle);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalCenter, _viewModel.Center);
        }

        [Fact]
        public void SetVisualBottomRightAngledTest()
        {
            SetupViewModel(GenerateAngledTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[0], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel activeKeyFrame = (CropKeyFrameViewModel)_viewModel.ActiveKeyFrame;

            Point originalVisualTopLeft = _viewModel.VisualTopLeft;
            Point originalVisualTopRight = _viewModel.VisualTopRight;
            Point originalVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point originalVisualBottomRight = _viewModel.VisualBottomRight;
            Point originalCenter = _viewModel.Center;
            double originalWidth = _viewModel.Width;
            double originalHeight = _viewModel.Height;
            double originalAngle = _viewModel.Angle;
            double originalDataLeft = _viewModel.DataLeft;
            double originalDataTop = _viewModel.DataTop;

            double expectedWidth = 405.3770703463295;
            double expectedHeight = 322.83895550777277;

            Point newVisualBottomRight = new Point(509.60524361476337, 427.81135619908946);
            _viewModel.VisualBottomRight = newVisualBottomRight;

            Point visualBottomRight = _viewModel.VisualBottomRight;
            Assert.Equal(newVisualBottomRight.X, visualBottomRight.X, DoubleEqualityPrecision);
            Assert.Equal(newVisualBottomRight.Y, visualBottomRight.Y, DoubleEqualityPrecision);

            Point newVisualTopLeft = _viewModel.VisualTopLeft;
            Point newVisualTopRight = _viewModel.VisualTopRight;
            Point newVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point newCenter = _viewModel.Center;

            Assert.Equal(originalVisualTopLeft.X, newVisualTopLeft.X, DoubleEqualityPrecision);
            Assert.Equal(originalVisualTopLeft.Y, newVisualTopLeft.Y, DoubleEqualityPrecision);

            Assert.Equal(593.16211380118034, newVisualTopRight.X, DoubleEqualityPrecision);
            Assert.Equal(115.97287134194437, newVisualTopRight.Y, DoubleEqualityPrecision);

            Assert.Equal(118.04106198184326, newVisualBottomLeft.X, DoubleEqualityPrecision);
            Assert.Equal(322.89204994559503, newVisualBottomLeft.Y, DoubleEqualityPrecision);

            Assert.Equal(expectedWidth, _viewModel.Width, DoubleEqualityPrecision);
            Assert.Equal(expectedHeight, _viewModel.Height, DoubleEqualityPrecision);
            Assert.Equal(originalAngle, _viewModel.Angle, DoubleEqualityPrecision);

            Assert.Equal(355.60158789151177, newCenter.X, DoubleEqualityPrecision);
            Assert.Equal(219.43246064376967, newCenter.Y, DoubleEqualityPrecision);

            Assert.Equal(152.91305271834705, _viewModel.DataLeft);
            Assert.Equal(58.0129828898833, _viewModel.DataTop);

            Assert.Equal(152.91305271834705, activeKeyFrame.Left);
            Assert.Equal(58.0129828898833, activeKeyFrame.Top);
            Assert.Equal(expectedWidth, activeKeyFrame.Width, DoubleEqualityPrecision);
            Assert.Equal(expectedHeight, activeKeyFrame.Height, DoubleEqualityPrecision);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);
            Assert.Equal(originalAngle, activeKeyFrame.Angle);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalCenter, _viewModel.Center);
        }

        [Fact]
        public void SetWidthAngledTest()
        {
            SetupViewModel(GenerateAngledTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[0], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel activeKeyFrame = (CropKeyFrameViewModel)_viewModel.ActiveKeyFrame;

            Point originalVisualTopLeft = _viewModel.VisualTopLeft;
            Point originalVisualTopRight = _viewModel.VisualTopRight;
            Point originalVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point originalVisualBottomRight = _viewModel.VisualBottomRight;
            Point originalCenter = _viewModel.Center;
            double originalWidth = _viewModel.Width;
            double originalHeight = _viewModel.Height;
            double originalAngle = _viewModel.Angle;
            double originalDataLeft = _viewModel.DataLeft;
            double originalDataTop = _viewModel.DataTop;

            double newWidth = 413d;
            _viewModel.Width = newWidth;

            Assert.Equal(newWidth, _viewModel.Width);

            Point newVisualTopLeft = _viewModel.VisualTopLeft;
            Point newVisualTopRight = _viewModel.VisualTopRight;
            Point newVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point newVisualBottomRight = _viewModel.VisualBottomRight;
            Point newCenter = _viewModel.Center;

            Assert.Equal(originalVisualTopLeft.X, newVisualTopLeft.X, DoubleEqualityPrecision);
            Assert.Equal(originalVisualTopLeft.Y, newVisualTopLeft.Y, DoubleEqualityPrecision);

            Assert.Equal(600.52529842564547, newVisualTopRight.X, DoubleEqualityPrecision);
            Assert.Equal(117.945830715791, newVisualTopRight.Y, DoubleEqualityPrecision);

            Assert.Equal(originalVisualBottomLeft.X, newVisualBottomLeft.X, DoubleEqualityPrecision);
            Assert.Equal(originalVisualBottomLeft.Y, newVisualBottomLeft.Y, DoubleEqualityPrecision);

            Assert.Equal(520.55021348896651, newVisualBottomRight.X, DoubleEqualityPrecision);
            Assert.Equal(416.41691103911307, newVisualBottomRight.Y, DoubleEqualityPrecision);

            Assert.Equal(newWidth, _viewModel.Width, DoubleEqualityPrecision);
            Assert.Equal(originalHeight, _viewModel.Height, DoubleEqualityPrecision);
            Assert.Equal(originalAngle, _viewModel.Angle, DoubleEqualityPrecision);

            Assert.Equal(361.07407282861334, newCenter.X, DoubleEqualityPrecision);
            Assert.Equal(213.73523806378148, newCenter.Y, DoubleEqualityPrecision);

            Assert.Equal(154.57407282861334, _viewModel.DataLeft);
            Assert.Equal(59.235238063781487, _viewModel.DataTop);

            Assert.Equal(154.57407282861334, activeKeyFrame.Left);
            Assert.Equal(59.235238063781487, activeKeyFrame.Top);
            Assert.Equal(newWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height, DoubleEqualityPrecision);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);
            Assert.Equal(originalAngle, activeKeyFrame.Angle);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalCenter, _viewModel.Center);
        }

        [Fact]
        public void SetHeightAngledTest()
        {
            SetupViewModel(GenerateAngledTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[0], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel activeKeyFrame = (CropKeyFrameViewModel)_viewModel.ActiveKeyFrame;

            Point originalVisualTopLeft = _viewModel.VisualTopLeft;
            Point originalVisualTopRight = _viewModel.VisualTopRight;
            Point originalVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point originalVisualBottomRight = _viewModel.VisualBottomRight;
            Point originalCenter = _viewModel.Center;
            double originalWidth = _viewModel.Width;
            double originalHeight = _viewModel.Height;
            double originalAngle = _viewModel.Angle;
            double originalDataLeft = _viewModel.DataLeft;
            double originalDataTop = _viewModel.DataTop;

            double newHeight = 334d;
            _viewModel.Height = newHeight;

            Assert.Equal(newHeight, _viewModel.Height, DoubleEqualityPrecision);

            Point newVisualTopLeft = _viewModel.VisualTopLeft;
            Point newVisualTopRight = _viewModel.VisualTopRight;
            Point newVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point newVisualBottomRight = _viewModel.VisualBottomRight;
            Point newCenter = _viewModel.Center;

            Assert.Equal(originalVisualTopLeft.X, newVisualTopLeft.X, DoubleEqualityPrecision);
            Assert.Equal(originalVisualTopLeft.Y, newVisualTopLeft.Y, DoubleEqualityPrecision);

            Assert.Equal(originalVisualTopRight.X, newVisualTopRight.X, DoubleEqualityPrecision);
            Assert.Equal(originalVisualTopRight.Y, newVisualTopRight.Y, DoubleEqualityPrecision);

            Assert.Equal(115.15237110401827, newVisualBottomLeft.X, DoubleEqualityPrecision);
            Assert.Equal(333.67279106899878, newVisualBottomLeft.Y, DoubleEqualityPrecision);

            Assert.Equal(489.93159170417675, newVisualBottomRight.X, DoubleEqualityPrecision);
            Assert.Equal(434.09458056877685, newVisualBottomRight.Y, DoubleEqualityPrecision);

            Assert.Equal(originalWidth, _viewModel.Width, DoubleEqualityPrecision);
            Assert.Equal(originalAngle, _viewModel.Angle, DoubleEqualityPrecision);

            Assert.Equal(345.76476193621846, newCenter.X, DoubleEqualityPrecision);
            Assert.Equal(222.5740728286134, newCenter.Y, DoubleEqualityPrecision);

            Assert.Equal(151.76476193621846, _viewModel.DataLeft);
            Assert.Equal(55.574072828613396, _viewModel.DataTop);

            Assert.Equal(151.76476193621846, activeKeyFrame.Left);
            Assert.Equal(55.574072828613396, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width, DoubleEqualityPrecision);
            Assert.Equal(newHeight, activeKeyFrame.Height, DoubleEqualityPrecision);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);
            Assert.Equal(originalAngle, activeKeyFrame.Angle);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalCenter, _viewModel.Center);
        }

        [Fact]
        public void SetCenterAngledTest()
        {
            SetupViewModel(GenerateAngledTestSegmentModel());

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[0], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            CropKeyFrameViewModel activeKeyFrame = (CropKeyFrameViewModel)_viewModel.ActiveKeyFrame;

            Vector centerSetVector = new Vector(50d, 50d);

            Point originalVisualTopLeft = _viewModel.VisualTopLeft;
            Point originalVisualTopRight = _viewModel.VisualTopRight;
            Point originalVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point originalVisualBottomRight = _viewModel.VisualBottomRight;
            Point originalCenter = _viewModel.Center;
            double originalWidth = _viewModel.Width;
            double originalHeight = _viewModel.Height;
            double originalAngle = _viewModel.Angle;
            double originalDataLeft = _viewModel.DataLeft;
            double originalDataTop = _viewModel.DataTop;

            Point newCenter = originalCenter + centerSetVector;
            _viewModel.Center = newCenter;

            Assert.Equal(newCenter, _viewModel.Center);

            Point newVisualTopLeft = _viewModel.VisualTopLeft;
            Point newVisualTopRight = _viewModel.VisualTopRight;
            Point newVisualBottomLeft = _viewModel.VisualBottomLeft;
            Point newVisualBottomRight = _viewModel.VisualBottomRight;

            Assert.Equal(originalVisualTopLeft.X + centerSetVector.X, newVisualTopLeft.X, DoubleEqualityPrecision);
            Assert.Equal(originalVisualTopLeft.Y + centerSetVector.Y, newVisualTopLeft.Y, DoubleEqualityPrecision);

            Assert.Equal(originalVisualTopRight.X + centerSetVector.X, newVisualTopRight.X, DoubleEqualityPrecision);
            Assert.Equal(originalVisualTopRight.Y + centerSetVector.Y, newVisualTopRight.Y, DoubleEqualityPrecision);

            Assert.Equal(originalVisualBottomLeft.X + centerSetVector.X, newVisualBottomLeft.X, DoubleEqualityPrecision);
            Assert.Equal(originalVisualBottomLeft.Y + centerSetVector.Y, newVisualBottomLeft.Y, DoubleEqualityPrecision);

            Assert.Equal(originalVisualBottomRight.X + centerSetVector.X, newVisualBottomRight.X, DoubleEqualityPrecision);
            Assert.Equal(originalVisualBottomRight.Y + centerSetVector.Y, newVisualBottomRight.Y, DoubleEqualityPrecision);

            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalDataLeft + centerSetVector.X, _viewModel.DataLeft);
            Assert.Equal(originalDataTop + centerSetVector.Y, _viewModel.DataTop);

            Assert.Equal(originalDataLeft + centerSetVector.X, activeKeyFrame.Left);
            Assert.Equal(originalDataTop + centerSetVector.Y, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);

            Assert.True(_undoRoot.CanUndo);
            _undoRoot.Undo();

            Assert.Equal(originalDataLeft, activeKeyFrame.Left);
            Assert.Equal(originalDataTop, activeKeyFrame.Top);
            Assert.Equal(originalWidth, activeKeyFrame.Width);
            Assert.Equal(originalHeight, activeKeyFrame.Height);
            Assert.Equal(originalAngle, activeKeyFrame.Angle);

            Assert.Equal(originalDataLeft, _viewModel.DataLeft);
            Assert.Equal(originalDataTop, _viewModel.DataTop);
            Assert.Equal(originalWidth, _viewModel.Width);
            Assert.Equal(originalHeight, _viewModel.Height);
            Assert.Equal(originalAngle, _viewModel.Angle);

            Assert.Equal(originalVisualTopLeft, _viewModel.VisualTopLeft);
            Assert.Equal(originalVisualTopRight, _viewModel.VisualTopRight);
            Assert.Equal(originalVisualBottomLeft, _viewModel.VisualBottomLeft);
            Assert.Equal(originalVisualBottomRight, _viewModel.VisualBottomRight);
            Assert.Equal(originalCenter, _viewModel.Center);
        }

        [Fact]
        public void LerpKeyFramesTest()
        {
            SetupViewModel(GenerateZeroAngleTestSegmentModel());
            IScriptVideoContext svc = _scriptVideoContextMock.Object;

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Angle,
                _viewModel.Angle);
            Assert.Equal(
                new Point(((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left,
                          ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top),
                _viewModel.VisualTopLeft);
            Assert.Equal(
                new Point(((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left + ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Width,
                          ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top),
                _viewModel.VisualTopRight);
            Assert.Equal(
                new Point(((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left,
                          ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top + ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Height),
                _viewModel.VisualBottomLeft);
            Assert.Equal(
                new Point(((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left + ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Width,
                          ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top + ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Height),
                _viewModel.VisualBottomRight);
            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Width,
                _viewModel.Width);
            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Height,
                _viewModel.Height);
            Assert.Equal(
                new Point(349d, 210.5),
                _viewModel.Center);
            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left,
                _viewModel.DataLeft);
            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top,
                _viewModel.DataTop);

            svc.FrameNumber = 5;
            Assert.Null(_viewModel.ActiveKeyFrame);
            Assert.False(_viewModel.CanBeEdited);

            Assert.Equal(0d, _viewModel.Angle);
            Assert.Equal(new Point(127.5, 118.5), _viewModel.VisualTopLeft);
            Assert.Equal(new Point(497d, 118.5), _viewModel.VisualTopRight);
            Assert.Equal(new Point(127.5, 388d), _viewModel.VisualBottomLeft);
            Assert.Equal(new Point(497d, 388d), _viewModel.VisualBottomRight);
            Assert.Equal(369.5, _viewModel.Width);
            Assert.Equal(269.5, _viewModel.Height);
            Assert.Equal(new Point(312.25, 253.25), _viewModel.Center);
            Assert.Equal(_viewModel.VisualTopLeft.X, _viewModel.DataLeft);
            Assert.Equal(_viewModel.VisualTopLeft.Y, _viewModel.DataTop);

            svc.FrameNumber = 10;
            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Angle,
                _viewModel.Angle);
            Assert.Equal(
                new Point(((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left,
                          ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top),
                _viewModel.VisualTopLeft);
            Assert.Equal(
                new Point(((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left + ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Width,
                          ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top),
                _viewModel.VisualTopRight);
            Assert.Equal(
                new Point(((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left,
                          ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top + ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Height),
                _viewModel.VisualBottomLeft);
            Assert.Equal(
                new Point(((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left + ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Width,
                          ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top + ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Height),
                _viewModel.VisualBottomRight);
            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Width,
                _viewModel.Width);
            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Height,
                _viewModel.Height);
            Assert.Equal(
                new Point(275.5, 296d),
                _viewModel.Center);
            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left,
                _viewModel.DataLeft);
            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top,
                _viewModel.DataTop);
        }

        [Fact]
        public void LerpAngledKeyFramesTest()
        {
            SetupViewModel(GenerateAngledTestSegmentModel());
            IScriptVideoContext svc = _scriptVideoContextMock.Object;

            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[0], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Angle,
                _viewModel.Angle);

            Assert.Equal(201.5979321682602, _viewModel.VisualTopLeft.X);
            Assert.Equal(11.053565088449915, _viewModel.VisualTopLeft.Y);

            Assert.Equal(576.37715276841868, _viewModel.VisualTopRight.X);
            Assert.Equal(111.47535458822797, _viewModel.VisualTopRight.Y);

            Assert.Equal(121.6228472315813, _viewModel.VisualBottomLeft.X);
            Assert.Equal(309.524645411772, _viewModel.VisualBottomLeft.Y);

            Assert.Equal(496.40206783173977, _viewModel.VisualBottomRight.X);
            Assert.Equal(409.94643491155006, _viewModel.VisualBottomRight.Y);

            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Width,
                _viewModel.Width);
            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Height,
                _viewModel.Height);

            Point centerPoint = _viewModel.Center;
            Assert.Equal(349d, centerPoint.X);
            Assert.Equal(210.5, centerPoint.Y);

            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left,
                _viewModel.DataLeft);
            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top,
                _viewModel.DataTop);

            svc.FrameNumber = 5;
            Assert.Null(_viewModel.ActiveKeyFrame);
            Assert.False(_viewModel.CanBeEdited);

            Assert.Equal(-4.5, _viewModel.Angle);

            Assert.Equal(102.8974989760159, _viewModel.VisualTopLeft.X, DoubleEqualityPrecision);
            Assert.Equal(76.696629568373609, _viewModel.VisualTopLeft.Y, DoubleEqualityPrecision);

            Assert.Equal(438.85864044408004, _viewModel.VisualTopRight.X, DoubleEqualityPrecision);
            Assert.Equal(50.255914308089892, _viewModel.VisualTopRight.Y, DoubleEqualityPrecision);

            Assert.Equal(127.14135955591996, _viewModel.VisualBottomLeft.X, DoubleEqualityPrecision);
            Assert.Equal(384.74408569191013, _viewModel.VisualBottomLeft.Y, DoubleEqualityPrecision);

            Assert.Equal(463.10250102398408, _viewModel.VisualBottomRight.X, DoubleEqualityPrecision);
            Assert.Equal(358.30337043162638, _viewModel.VisualBottomRight.Y, DoubleEqualityPrecision);

            Assert.Equal(337d, _viewModel.Width, DoubleEqualityPrecision);
            Assert.Equal(309d, _viewModel.Height, DoubleEqualityPrecision);

            centerPoint = _viewModel.Center;
            Assert.Equal(283d, centerPoint.X);
            Assert.Equal(217.5, centerPoint.Y);
            
            Assert.Equal(114.5, _viewModel.DataLeft, DoubleEqualityPrecision);
            Assert.Equal(63d, _viewModel.DataTop, DoubleEqualityPrecision);

            svc.FrameNumber = 10;
            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[1], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Angle,
                _viewModel.Angle);

            Assert.Equal(23.522188201896952, _viewModel.VisualTopLeft.X);
            Assert.Equal(141.5205667540576, _viewModel.VisualTopLeft.Y);

            Assert.Equal(284.79618908768083, _viewModel.VisualTopRight.X);
            Assert.Equal(25.193886834378738, _viewModel.VisualTopRight.Y);

            Assert.Equal(149.20381091231923, _viewModel.VisualBottomLeft.X);
            Assert.Equal(423.80611316562124, _viewModel.VisualBottomLeft.Y);

            Assert.Equal(410.47781179810306, _viewModel.VisualBottomRight.X);
            Assert.Equal(307.4794332459424, _viewModel.VisualBottomRight.Y);

            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Width,
                _viewModel.Width);
            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Height,
                _viewModel.Height);

            centerPoint = _viewModel.Center;
            Assert.Equal(217d, centerPoint.X);
            Assert.Equal(224.5, centerPoint.Y);

            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left,
                _viewModel.DataLeft);
            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top,
                _viewModel.DataTop);

            svc.FrameNumber = 15;
            Assert.Null(_viewModel.ActiveKeyFrame);
            Assert.False(_viewModel.CanBeEdited);

            Assert.Equal(-12d, _viewModel.Angle);

            Assert.Equal(86.059773045009933, _viewModel.VisualTopLeft.X, DoubleEqualityPrecision);
            Assert.Equal(101.40931558941946, _viewModel.VisualTopLeft.Y, DoubleEqualityPrecision);

            Assert.Equal(415.69551449230249, _viewModel.VisualTopRight.X, DoubleEqualityPrecision);
            Assert.Equal(31.343075783834614, _viewModel.VisualTopRight.Y, DoubleEqualityPrecision);

            Assert.Equal(150.30448550769754, _viewModel.VisualBottomLeft.X, DoubleEqualityPrecision);
            Assert.Equal(403.65692421616535, _viewModel.VisualBottomLeft.Y, DoubleEqualityPrecision);

            Assert.Equal(479.94022695499007, _viewModel.VisualBottomRight.X, DoubleEqualityPrecision);
            Assert.Equal(333.59068441058048, _viewModel.VisualBottomRight.Y, DoubleEqualityPrecision);

            Assert.Equal(337d, _viewModel.Width, DoubleEqualityPrecision);
            Assert.Equal(309d, _viewModel.Height, DoubleEqualityPrecision);

            centerPoint = _viewModel.Center;
            Assert.Equal(283d, centerPoint.X, DoubleEqualityPrecision);
            Assert.Equal(217.5, centerPoint.Y, DoubleEqualityPrecision);

            Assert.Equal(114.5, _viewModel.DataLeft, DoubleEqualityPrecision);
            Assert.Equal(63d, _viewModel.DataTop, DoubleEqualityPrecision);

            svc.FrameNumber = 20;
            Assert.NotNull(_viewModel.ActiveKeyFrame);
            Assert.Equal(_viewModel.KeyFrameViewModels[2], _viewModel.ActiveKeyFrame);
            Assert.True(_viewModel.CanBeEdited);

            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Angle,
                _viewModel.Angle);
            Assert.Equal(
                new Point(((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left,
                          ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top),
                _viewModel.VisualTopLeft);
            Assert.Equal(
                new Point(((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left + ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Width,
                          ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top),
                _viewModel.VisualTopRight);
            Assert.Equal(
                new Point(((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left,
                          ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top + ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Height),
                _viewModel.VisualBottomLeft);
            Assert.Equal(
                new Point(((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left + ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Width,
                          ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top + ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Height),
                _viewModel.VisualBottomRight);
            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Width,
                _viewModel.Width);
            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Height,
                _viewModel.Height);

            centerPoint = _viewModel.Center;
            Assert.Equal(349d, centerPoint.X);
            Assert.Equal(210.5, centerPoint.Y);

            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Left,
                _viewModel.DataLeft);
            Assert.Equal(
                ((CropKeyFrameViewModel)_viewModel.ActiveKeyFrame).Top,
                _viewModel.DataTop);
        }
    }
}