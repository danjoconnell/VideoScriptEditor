#pragma once

#define WIN32_LEAN_AND_MEAN         // Exclude rarely-used stuff from Windows headers
// Windows Header Files
#include <windows.h>
#include <d2d1_3.h>
#include <wrl/client.h>
#include <wincodec.h>

#pragma warning(push)
#pragma warning(disable : 26495)    // C26495: Variable '%variable%' is uninitialized. Always initialize a member variable.
#include <avisynth.h>
#pragma warning(pop)

#include <libyuv.h>
#include <tinyxml2.h>
#include <fmt/format.h>

#include <memory>
#include <string>
#include <map>
#include <vector>
#include <string_view>
#include <tuple>
#include <algorithm>
#include <stdexcept>
#include <cmath>
#include <cassert>

#include "..\..\Shared\cpp\Primitives.h"
#include "..\..\Shared\cpp\CommonDataStructs.h"
#include "..\..\Shared\cpp\CommonFunctionTemplates.h"
#include "MathHelpers.h"
#include "VSEProject.h"

typedef Microsoft::WRL::ComPtr<ID2D1Geometry> ID2D1GeometryPtr;

#define PLUGIN_NAME "VSEProcessorAviSynth"

constexpr int YV12_MOD_FACTOR = 2;