#include "pch.h"
#include "..\VSEProcessorAviSynth\VSEProjectFileParser.h"

namespace UnitTests
{
    constexpr auto PROJECT_FILE_PATH = R"(TestFiles\MultiCropMaskingNoRotation.vseproj)";

    TEST(VSEProjectFileParserTest, Parse)
    {
        VSEProject testProject;
        VSEProjectFileParser testProjectFileParser(testProject);

        ASSERT_NO_THROW(testProjectFileParser.Parse(PROJECT_FILE_PATH));

        // TODO: Verify testProject content is correct.
    }
}