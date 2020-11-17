#include "pch.h"
#include <string>
#include "AviSynthEnvironment.h"
#include <system_error>

namespace VideoScriptEditor::PreviewRenderer::Unmanaged
{
    using namespace VideoScriptEditor::Unmanaged;

    AviSynthEnvironment::AviSynthEnvironment() : AviSynthEnvironmentBase()
    {
        if (!CreateScriptEnvironment())
        {
            throw std::runtime_error("Failed to initialize AviSynth Script Environment");
        }
    }

    AviSynthEnvironment::~AviSynthEnvironment()
    {
        // Falls through to base class destructor
    }

    void AviSynthEnvironment::ResetEnvironment()
    {
        // Unload the current environment
        DeleteScriptEnvironment();

        // Initialize a new environment
        if (!CreateScriptEnvironment())
        {
            throw std::runtime_error("Failed to initialize AviSynth Script Environment");
        }
    }

    bool AviSynthEnvironment::LoadScriptFromFile(const std::string& fileName)
    {
        if (_clip)
        {
            ResetEnvironment();
        }

        // Invoke the AviSynth Import source filter to load the file - see http://avisynth.nl/index.php/Import#Import
        AVSValue avsImportResult;
        if (!_scriptEnvironment->InvokeTry(&avsImportResult, "Import", AVSValue(fileName.c_str())) || !avsImportResult.IsClip())
        {
            return false;
        }

        _clip = avsImportResult.AsClip();

        return true;
    }
}