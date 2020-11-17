#if !defined(WIN32_LEAN_AND_MEAN)
#define WIN32_LEAN_AND_MEAN         // Exclude rarely-used stuff from Windows headers
#endif

#include <windows.h>

#pragma warning(push)
#pragma warning(disable : 26495)    // C26495: Variable '%variable%' is uninitialized. Always initialize a member variable.
#include <avisynth.h>
#pragma warning(pop)

#include "AviSynthEnvironmentBase.h"
#include <system_error>

// See http://www.avisynth.nl/index.php/Filter_SDK/AVS_Linkage
const AVS_Linkage* AVS_linkage = nullptr;

namespace VideoScriptEditor::Unmanaged
{
    using namespace std;

    AviSynthEnvironmentBase::AviSynthEnvironmentBase()
        : _aviSynthDllHandle(LoadLibraryW(L"avisynth.dll"))
    {
        ZeroMemory(&_clip, sizeof(PClip));

        if (_aviSynthDllHandle == nullptr)
        {
            // https://stackoverflow.com/questions/4475157/turning-getlasterror-into-an-exception/40493858#40493858
            throw system_error(GetLastError(), system_category(), "Failed to load avisynth.dll");
        }

        _AviSynthCreateScriptEnvironment = reinterpret_cast<CreateScriptEnvironmentFuncPtr>(GetProcAddress(_aviSynthDllHandle, "CreateScriptEnvironment"));
        if (_AviSynthCreateScriptEnvironment == nullptr)
        {
            throw system_error(GetLastError(), system_category(), "Failed to find 'CreateScriptEnvironment' function export in avisynth.dll");
        }

        _scriptEnvironment = nullptr;
    }

    AviSynthEnvironmentBase::~AviSynthEnvironmentBase()
    {
        DeleteScriptEnvironment();

        _AviSynthCreateScriptEnvironment = nullptr;
    }

    bool AviSynthEnvironmentBase::CreateScriptEnvironment()
    {
        _scriptEnvironment = _AviSynthCreateScriptEnvironment(AVISYNTH_INTERFACE_VERSION);
        if (_scriptEnvironment == nullptr)
        {
            return false;
        }

        AVS_linkage = _scriptEnvironment->GetAVSLinkage();
        return true;
    }

    void AviSynthEnvironmentBase::DeleteScriptEnvironment()
    {
        // Unload the current environment
        _clip = nullptr;
        AVS_linkage = nullptr;

        if (_scriptEnvironment != nullptr)
        {
            // Calling DeleteScriptEnvironment on an IScriptEnvironment instance results in a dangling instance pointer.
            _scriptEnvironment->DeleteScriptEnvironment();

            // Explicitly setting the instance pointer to null here so it doesn't point to freed memory.
            _scriptEnvironment = nullptr;
        }

        ZeroMemory(&_clip, sizeof(PClip));
    }

    PVideoFrame AviSynthEnvironmentBase::GetVideoFrame(const int frameNumber)
    {
        return _clip ? _clip->GetFrame(frameNumber, _scriptEnvironment) : nullptr;
    }
}