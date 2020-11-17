#pragma once
#include "SafeModuleHandle.h"

namespace VideoScriptEditor::Unmanaged
{
    // Function pointer typedef for the AviSynth.dll exported CreateScriptEnvironment function.
    // See http://www.avisynth.nl/index.php/Filter_SDK/Cplusplus_API#CreateScriptEnvironment
    typedef IScriptEnvironment* (__stdcall* CreateScriptEnvironmentFuncPtr)(int);

    /// <summary>
    /// A wrapper class for loading and interacting with the AviSynth frameserving script environment.
    /// </summary>
    class AviSynthEnvironmentBase
    {
    protected:
        SafeModuleHandle _aviSynthDllHandle;
        CreateScriptEnvironmentFuncPtr _AviSynthCreateScriptEnvironment;
        IScriptEnvironment* _scriptEnvironment;
        PClip _clip;

    public:
        /* Properties */

        /// <summary>
        /// Gets a value that indicates whether a script is loaded in the current environment.
        /// </summary>
        /// <returns>True if a script is loaded (Clip is non-null), false otherwise.</returns>
        const bool get_HasLoadedScript() { return _clip; }

        /// <summary>
        /// Gets a pointer to the AviSynth exported Script Environment.
        /// See http://www.avisynth.nl/index.php/Filter_SDK/Cplusplus_API
        /// </summary>
        /// <returns>A pointer to the IScriptEnvironment singleton interface.</returns>
        const IScriptEnvironment* get_ScriptEnvironment() { return _scriptEnvironment; }

        /// <summary>
        /// Gets the Clip representing the loaded script.
        /// </summary>
        /// <returns>A smart pointer (PClip) to the Clip.</returns>
        const PClip& get_Clip() { return _clip; }

        /// <summary>
        /// Gets a pointer to a <see cref="VideoInfo"/> structure which contains information about the Clip.
        /// See http://www.avisynth.nl/index.php/Filter_SDK/Cplusplus_API/VideoInfo
        /// </summary>
        /// <returns>A pointer to a <see cref="VideoInfo"/> structure or nullptr if Clip is null.</returns>
        const VideoInfo* get_VideoInfo() { return _clip ? &_clip->GetVideoInfo() : nullptr; }

    protected:
        /// <summary>
        /// Base constructor for classes derived from the <see cref="AviSynthEnvironmentBase"/> class.
        /// </summary>
        AviSynthEnvironmentBase();

    public:
        /// <summary>
        /// Base destructor for classes derived from the <see cref="AviSynthEnvironmentBase"/> class.
        /// </summary>
        virtual ~AviSynthEnvironmentBase();

        /// <summary>
        /// Creates the AviSynth frameserving script environment (IScriptEnvironment).
        /// </summary>
        /// <returns>A boolean value indicating success or failure.</returns>
        bool CreateScriptEnvironment();

        /// <summary>
        /// Deletes the AviSynth frameserving script environment (IScriptEnvironment) from memory.
        /// </summary>
        void DeleteScriptEnvironment();

        /// <summary>
        /// Gets a smart pointer to a video frame from the Clip.
        /// </summary>
        /// <param name="frameNumber">The zero-based number of the frame to return.</param>
        /// <returns>A smart pointer (PVideoFrame) to a video frame.</returns>
        PVideoFrame GetVideoFrame(const int frameNumber);
    };
}