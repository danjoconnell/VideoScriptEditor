#pragma once
#include "..\..\Shared\cpp\AviSynthEnvironmentBase.h"

/// <summary>
/// A simple AviSynth runtime environment for testing AviSynth filters/plugins.
/// </summary>
class AviSynthTestEnvironment : public VideoScriptEditor::Unmanaged::AviSynthEnvironmentBase
{
public:
    /// <summary>
    /// Constructor for the AviSynthTestEnvironment class
    /// </summary>
    AviSynthTestEnvironment();

    /// <summary>
    /// Destructor for the AviSynthTestEnvironment class
    /// </summary>
    ~AviSynthTestEnvironment();

    /// <summary>
    /// Loads an AviSynth script from a <see cref="std::string"/> into the test environment.
    /// </summary>
    /// <param name="scriptBody">
    /// A reference to a <see cref="std::string"/> containing the body of the AviSynth script to load.
    /// </param>
    /// <returns>A boolean value indicating whether the script was loaded.</returns>
    bool LoadScriptFromString(const std::string& scriptBody);

    /// <summary>
    /// Requests the specified frame from the loaded script.
    /// </summary>
    /// <param name="frameNumber">The requested frame number.</param>
    /// <returns>A boolean value indicating success or failure.</returns>
    bool RequestFrame(const int frameNumber);
};

