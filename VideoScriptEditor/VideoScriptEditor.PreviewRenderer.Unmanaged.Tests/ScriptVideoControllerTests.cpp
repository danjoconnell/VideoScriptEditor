#include "pch.h"
#include "..\VideoScriptEditor.PreviewRenderer.Unmanaged\ScriptVideoController.h"

using namespace VideoScriptEditor::PreviewRenderer::Unmanaged;
using namespace std;

namespace VideoScriptEditor::PreviewRenderer::Unmanaged::Tests
{
    constexpr auto AVS_TEST_SCRIPT_FILE_PATH = R"(TestFiles\AVSSourceTestScript-628x472-23.976fps.avs)";

    class ScriptVideoControllerTestFixture : public ::testing::Test
    {
    protected:
        std::unique_ptr<ScriptVideoController> _scriptVideoController;

        void SetUp() override
        {
            _scriptVideoController = std::make_unique<ScriptVideoController>();
        }

        void TearDown() override
        {
            _scriptVideoController = nullptr;
        }
    };

    TEST_F(ScriptVideoControllerTestFixture, LoadAviSynthScriptFromFile)
    {
        LoadedScriptVideoInfo loadedScriptVideoInfo = _scriptVideoController->LoadAviSynthScriptFromFile(AVS_TEST_SCRIPT_FILE_PATH);
        ASSERT_TRUE(loadedScriptVideoInfo.HasVideo);
    }

    TEST_F(ScriptVideoControllerTestFixture, RenderSourceFrameSurface)
    {
        LoadedScriptVideoInfo loadedScriptVideoInfo = _scriptVideoController->LoadAviSynthScriptFromFile(AVS_TEST_SCRIPT_FILE_PATH);
        ASSERT_TRUE(loadedScriptVideoInfo.HasVideo);

        _scriptVideoController->RenderSourceFrameSurface(0);
    }
}