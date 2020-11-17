using Moq;
using System;
using System.Threading.Tasks;
using VideoScriptEditor.Models;
using VideoScriptEditor.Services.Dialog;
using VideoScriptEditor.Services.ScriptVideo;
using Xunit;

namespace VideoScriptEditor.Services.Tests
{
    public class ScriptVideoServiceTests : IDisposable
    {
        private readonly string AVS_TEST_SCRIPT_FILE_PATH = @"TestFiles\AVSSourceTestScript-628x472-23.976fps.avs";
        private readonly string PROJECT_FILE_PATH = @"TestFiles\MultiCropMaskingNoRotation.vseproj";

        private IProjectService _projectService;
        private Mock<ISystemDialogService> _systemDialogServiceMock;
        private ScriptVideoService _scriptVideoService;
        private IScriptVideoContext _scriptVideoContext;
        private ProjectModel _project;

        public ScriptVideoServiceTests()
        {
            _projectService = new ProjectService();

            _systemDialogServiceMock = new Mock<ISystemDialogService>();
            _systemDialogServiceMock.Setup(sds => sds.ShowErrorDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Exception>()))
                                    .Callback<string, string, Exception>((dialogTextParam, dialogCaptionParam, exceptionParam) =>
                                    {
                                        throw exceptionParam ?? new Exception(dialogTextParam);
                                    });

            _scriptVideoService = new ScriptVideoService(_systemDialogServiceMock.Object);
            _scriptVideoContext = _scriptVideoService.GetContextReference();
        }

        public void Dispose()
        {
            _scriptVideoContext = null;

            if (_scriptVideoService != null)
            {
                _scriptVideoService.Dispose();
                _scriptVideoService = null;
            }

            if (_project != null)
            {
                _projectService.CloseProject();
                _project = null;
            }
        }

        [Fact]
        public void LoadScriptFromFileSourceTest()
        {
            Assert.True(System.IO.File.Exists(AVS_TEST_SCRIPT_FILE_PATH));

            _project = _projectService.CreateNewProject();

            _scriptVideoContext.Project = _project;
            Assert.True(
                string.IsNullOrWhiteSpace(_scriptVideoContext.ScriptFileSource)
            );

            Assert.False(_scriptVideoContext.HasVideo);

            _scriptVideoService.LoadScriptFromFileSource(AVS_TEST_SCRIPT_FILE_PATH);

            Assert.Equal(AVS_TEST_SCRIPT_FILE_PATH, _scriptVideoContext.ScriptFileSource);
            Assert.True(_scriptVideoContext.HasVideo);
        }

        [Fact]
        public void LoadScriptFromProjectTest()
        {
            Assert.True(System.IO.File.Exists(PROJECT_FILE_PATH));

            _project = _projectService.OpenProject(PROJECT_FILE_PATH);
            Assert.NotNull(_project);

            _scriptVideoContext.Project = _project;

            Assert.True(_scriptVideoContext.HasVideo);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(25)]
        public void SeekFrameTest(int frameNumber)
        {
            LoadScriptFromProjectTest();

            _scriptVideoService.SeekFrame(frameNumber);
            Assert.Equal(frameNumber, _scriptVideoContext.FrameNumber);
        }

        [Fact]
        public async Task PlayVideoTest()
        {
            LoadScriptFromProjectTest();
            await _scriptVideoService.StartVideoPlayback();
        }
    }
}
