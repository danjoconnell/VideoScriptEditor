#pragma once

#pragma managed(push, off)

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
// Windows Header Files
#include <windows.h>
#include <d3d9.h>
#include <d2d1.h>

#include <memory>
#include <map>
#include <vector>
#include <string>

/* Per https://github.com/Microsoft/DirectXTK/wiki/ComPtr,
   while the Windows Runtime C++ Template Library (WRL) ComPtr smart pointer has no runtime dependency on the Windows Runtime
   and therefore can be freely included in regular non-Windows Runtime C++ compiled code (even for Win7 targets),
   it cannot be compiled with the /clr option enabled (conditional compilation error defined in wrl/def.h).
   So, the following define instructs the compiler to use the older _com_ptr_t class template (https://docs.microsoft.com/en-us/cpp/cpp/com-ptr-t-class?view=vs-2019)
   as an alternative COM smart pointer when compiling unmanaged static library code/headers that will be externally referenced by /clr compiled code.
   Code internal to the unmanaged static library will still safely use Microsoft::WRL::ComPtr. */
#define CPPCLI_LINKAGE_RESTRICTIONS

#include <comdef.h>
_COM_SMARTPTR_TYPEDEF(IDirect3DSurface9, __uuidof(IDirect3DSurface9));
_COM_SMARTPTR_TYPEDEF(ID2D1Geometry, __uuidof(ID2D1Geometry));

#include "..\..\Shared\cpp\Primitives.h"
#include "..\..\Shared\cpp\CommonDataStructs.h"
#include "..\VideoScriptEditor.PreviewRenderer.Unmanaged\DataStructs.h"
#include "..\VideoScriptEditor.PreviewRenderer.Unmanaged\ScriptVideoController.h"

#pragma managed(pop)
