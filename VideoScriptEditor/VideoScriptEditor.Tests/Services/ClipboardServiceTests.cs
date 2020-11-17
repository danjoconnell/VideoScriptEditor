using System;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Tests.Mocks.MockModels;
using Xunit;

namespace VideoScriptEditor.Services.Tests
{
    public class ClipboardServiceTests : IDisposable
    {
        private ClipboardService _clipboardService;
        private MockSegmentModel _testSegmentModel;

        public ClipboardServiceTests()
        {
            _clipboardService = new ClipboardService();

            _testSegmentModel = new MockSegmentModel(0, 22, 0,
                                    new KeyFrameModelCollection()
                                    {
                                        new MockKeyFrameModel(0),
                                        new MockKeyFrameModel(10),
                                        new MockKeyFrameModel(22)
                                    },
                                    "Mock Segment"
                               );
        }

        public void Dispose()
        {
            _clipboardService?.Dispose();
            _clipboardService = null;
        }


        [StaFact]
        public void SetDataTest()
        {
            _clipboardService.SetData((MockKeyFrameModel)_testSegmentModel.KeyFrames[1]);
        }

        [StaFact]
        public void ContainsDataTest()
        {
            SetDataTest();
            Assert.True(
                _clipboardService.ContainsData<MockKeyFrameModel>()
            );
        }

        [StaFact]
        public void GetDataTest()
        {
            ContainsDataTest();

            MockKeyFrameModel pastedKeyFrame = _clipboardService.GetData<MockKeyFrameModel>();
            Assert.NotNull(pastedKeyFrame);
            Assert.Equal(0, _testSegmentModel.KeyFrames[1].CompareTo(pastedKeyFrame));
        }
    }
}