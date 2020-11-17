#include "pch.h"
#include "AviSynthTestEnvironment.h"

using namespace VideoScriptEditor::Unmanaged;

AviSynthTestEnvironment::AviSynthTestEnvironment() : AviSynthEnvironmentBase()
{
}

AviSynthTestEnvironment::~AviSynthTestEnvironment()
{
    // Falls through to base class destructor
}

bool AviSynthTestEnvironment::LoadScriptFromString(const std::string& scriptBody)
{
    if (_clip != nullptr)
    {
        DeleteScriptEnvironment();
        if (!this->CreateScriptEnvironment())
        {
            return false;
        }
    }

    AVSValue invokeResult;
    if (_scriptEnvironment->InvokeTry(&invokeResult, "Eval", AVSValue(scriptBody.c_str())))
    {
        _clip = invokeResult.AsClip();
    }

    return (_clip != nullptr);
}

bool AviSynthTestEnvironment::RequestFrame(const int frameNumber)
{
    return GetVideoFrame(frameNumber) != nullptr;
}
