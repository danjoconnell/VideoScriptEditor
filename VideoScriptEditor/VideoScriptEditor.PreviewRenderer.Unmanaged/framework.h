#pragma once
#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers

// Windows Header Files
#include <windows.h>
#include <d2d1_3.h>
#include <d3d9.h>
#include <d3d11_4.h>
#include <comdef.h>
#include <wrl/client.h>

#pragma warning(push)
#pragma warning(disable : 26495)    // C26495: Variable '%variable%' is uninitialized. Always initialize a member variable.
#include <avisynth.h>
#pragma warning(pop)

#include <string>
#include <memory>
#include <map>
#include <vector>

#include "..\..\Shared\cpp\ComHelpers.h"
#include "..\..\Shared\cpp\Primitives.h"
#include "..\..\Shared\cpp\CommonDataStructs.h"
#include "..\..\Shared\cpp\CommonFunctionTemplates.h"
#include "DataStructs.h"

_COM_SMARTPTR_TYPEDEF(IDirect3DSurface9, __uuidof(IDirect3DSurface9));
_COM_SMARTPTR_TYPEDEF(ID2D1Geometry, __uuidof(ID2D1Geometry));