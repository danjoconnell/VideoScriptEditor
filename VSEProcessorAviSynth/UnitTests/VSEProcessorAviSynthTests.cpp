#include "pch.h"
#include "AviSynthTestEnvironment.h"
#include <fmt/format.h>

namespace UnitTests
{
    using namespace std;

    constexpr auto PROJECT_FILE_PATH = R"(TestFiles\MultiCropMaskingNoRotation.vseproj)";

    constexpr auto TEST_SCRIPT =
R"(LoadPlugin("VSEProcessorAviSynth.dll")
ColorBars(640, 480, "YV12").AssumeFPS("ntsc_video").KillAudio()
Trim(0, 400)
Info()
VSEProcessorAviSynth("{:s}")
)";

    class VSEProcessorAviSynthTestFixture : public ::testing::Test
    {
    protected:
        static std::unique_ptr<AviSynthTestEnvironment> s_aviSynthTestEnv;

        // Per-test-suite set-up.
        // Called before the first test in this test suite.
        static void SetUpTestCase()
        {
            s_aviSynthTestEnv = std::make_unique<AviSynthTestEnvironment>();
        }

        // Per-test-suite tear-down.
        // Called after the last test in this test suite.
        static void TearDownTestCase()
        {
            s_aviSynthTestEnv = nullptr;
        }

        // Per-test set-up logic.
        void SetUp() override
        {
            ASSERT_NE(s_aviSynthTestEnv.get(), nullptr);

            ASSERT_TRUE(
                s_aviSynthTestEnv->CreateScriptEnvironment()
            );
        }

        // Per-test tear-down logic.
        void TearDown() override
        {
            ASSERT_NE(s_aviSynthTestEnv.get(), nullptr);

            s_aviSynthTestEnv->DeleteScriptEnvironment();
        }

        void LoadAvsEnvironmentTestScript()
        {
            ASSERT_TRUE(
                s_aviSynthTestEnv->LoadScriptFromString(fmt::format(TEST_SCRIPT, PROJECT_FILE_PATH))
            );

            ASSERT_TRUE(
                s_aviSynthTestEnv->get_HasLoadedScript()
            );

            const VideoInfo* vi = s_aviSynthTestEnv->get_VideoInfo();
            ASSERT_TRUE(vi->HasVideo());
        }
    };

    std::unique_ptr<AviSynthTestEnvironment> VSEProcessorAviSynthTestFixture::s_aviSynthTestEnv = nullptr;

    TEST_F(VSEProcessorAviSynthTestFixture, AvsEnvironmentLoadScriptFromString)
    {
        ASSERT_NO_FATAL_FAILURE(LoadAvsEnvironmentTestScript());
    }

    TEST_F(VSEProcessorAviSynthTestFixture, GetFrame)
    {
        ASSERT_NO_FATAL_FAILURE(LoadAvsEnvironmentTestScript());

        ASSERT_NO_THROW(s_aviSynthTestEnv->RequestFrame(0));
        ASSERT_NO_THROW(s_aviSynthTestEnv->RequestFrame(1));
        ASSERT_NO_THROW(s_aviSynthTestEnv->RequestFrame(15));
        ASSERT_NO_THROW(s_aviSynthTestEnv->RequestFrame(23));
        ASSERT_NO_THROW(s_aviSynthTestEnv->RequestFrame(100));
        ASSERT_NO_THROW(s_aviSynthTestEnv->RequestFrame(150));
        ASSERT_NO_THROW(s_aviSynthTestEnv->RequestFrame(269));
        ASSERT_NO_THROW(s_aviSynthTestEnv->RequestFrame(350));
    }
}
