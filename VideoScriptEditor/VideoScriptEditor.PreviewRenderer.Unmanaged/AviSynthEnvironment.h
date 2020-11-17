#pragma once
#include "..\..\Shared\cpp\AviSynthEnvironmentBase.h"

namespace VideoScriptEditor::PreviewRenderer::Unmanaged
{
    /// <summary>
    /// A wrapper class for loading and interacting with the AviSynth frameserving script environment
    /// </summary>
    class AviSynthEnvironment : public VideoScriptEditor::Unmanaged::AviSynthEnvironmentBase
    {
    public:
        /// <summary>
        /// Constructor for the AviSynthEnvironment class
        /// </summary>
        AviSynthEnvironment();

        /// <summary>
        /// Destructor for the AviSynthEnvironment class
        /// </summary>
        ~AviSynthEnvironment();

        /// <summary>
        /// Resets the environment by unloading and reinitializing it.
        /// </summary>
        void ResetEnvironment();

        /// <summary>
        /// Loads an AviSynth script from a file into the environment.
        /// </summary>
        /// <param name="fileName">A reference to a <see cref="std::string"/> containing the absolute AviSynth script file name</param>
        /// <returns>A boolean value indicating whether the script was loaded</returns>
        /// <remarks>Utilizes the AviSynth Import source filter which doesn't support relative file paths.</remarks>
        bool LoadScriptFromFile(const std::string& fileName);
    };
}
