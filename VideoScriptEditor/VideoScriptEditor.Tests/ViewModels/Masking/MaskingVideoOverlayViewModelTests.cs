using MonitoredUndo;
using Moq;
using System;
using VideoScriptEditor.Commands;
using VideoScriptEditor.Services;
using VideoScriptEditor.Services.Dialog;
using VideoScriptEditor.Services.ScriptVideo;
using VideoScriptEditor.Tests.Mocks;
using Xunit;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.ViewModels.Masking.Tests
{
    public class MaskingVideoOverlayViewModelTests
    {
        private readonly Mock<IScriptVideoService> _scriptVideoServiceMock;
        private readonly Mock<IScriptVideoContext> _scriptVideoContextMock;

        private readonly Mock<IUndoService> _undoServiceMock;
        private readonly Mock<IChangeFactory> _undoChangeFactoryMock;
        private UndoRoot _undoRoot;

        private readonly Mock<ISystemDialogService> _systemDialogServiceMock;
        private readonly Mock<IClipboardService> _clipboardServiceMock;

        private readonly ApplicationCommands _applicationCommands;

        private readonly Mock<IProjectService> _projectServiceMock;
        private Models.ProjectModel _mockMaskingProject;
        private MaskingVideoOverlayViewModel _viewModel;

        public MaskingVideoOverlayViewModelTests()
        {
            _scriptVideoContextMock = new Mock<IScriptVideoContext>();
            _scriptVideoContextMock.Setup(svc => svc.HasVideo).Returns(true);
            _scriptVideoContextMock.Setup(svc => svc.FrameNumber).Returns(0);
            _scriptVideoContextMock.Setup(svc => svc.IsVideoPlaying).Returns(false);
            _scriptVideoContextMock.Setup(svc => svc.VideoFrameCount).Returns(241);
            _scriptVideoContextMock.Setup(svc => svc.SeekableVideoFrameCount).Returns(240);
            _scriptVideoContextMock.Setup(svc => svc.VideoFrameSize).Returns(new SizeI(628, 472));

            _scriptVideoServiceMock = new Mock<IScriptVideoService>();
            _scriptVideoServiceMock.Setup(svs => svs.GetContextReference()).Returns(_scriptVideoContextMock.Object);

            _mockMaskingProject = MockProjectFactory.CreateMockMaskingProject();
            _projectServiceMock = new Mock<IProjectService>();
            _projectServiceMock.Setup(ps => ps.Project).Returns(_mockMaskingProject);

            _undoServiceMock = new Mock<IUndoService>();
            _undoServiceMock.Setup(us => us[It.IsAny<object>()]).Returns((object root) =>
            {
                if (_undoRoot?.Root != root)
                {
                    _undoRoot = new UndoRoot(root);
                }
                return _undoRoot;
            });

            _undoChangeFactoryMock = new Mock<IChangeFactory>();

            _systemDialogServiceMock = new Mock<ISystemDialogService>();
            _systemDialogServiceMock.Setup(sds => sds.ShowErrorDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Exception>()))
                                    .Callback<string, string, Exception>((dialogTextParam, dialogCaptionParam, exceptionParam) =>
                                    {
                                        throw exceptionParam ?? new Exception(dialogTextParam);
                                    });

            _clipboardServiceMock = new Mock<IClipboardService>();

            _applicationCommands = new ApplicationCommands();

            _viewModel = new MaskingVideoOverlayViewModel(_scriptVideoServiceMock.Object, _undoServiceMock.Object, _undoChangeFactoryMock.Object, _applicationCommands, _projectServiceMock.Object, _systemDialogServiceMock.Object, _clipboardServiceMock.Object);
        }

        [Fact]
        public void OnNavigatedToTest()
        {
            _viewModel.OnNavigatedTo(null);

            Assert.Equal(2, _viewModel.ActiveSegments.Count);
        }
    }
}