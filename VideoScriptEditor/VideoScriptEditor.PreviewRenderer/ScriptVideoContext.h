#pragma once

// Disable warnings such as 'C4691: 'System::Object': type referenced was expected in unreferenced assembly 'netstandard', type defined in current translation unit used instead'
// when importing types from Prism.dll.
// These warnings should be fixed when the Prism team updates their projects to properly support .Net 5.
#pragma warning( push )
#pragma warning( disable : 4691 )

namespace VideoScriptEditor::Services::ScriptVideo
{
    /// <summary>
    /// A representation of the runtime context of an <see cref="IScriptVideoService"/>.
    /// </summary>
    /// <remarks>
    /// The ScriptVideoContext is essentially split up into two parts;
    /// A C# base class containing purely managed code and not practical for coding in C++/CLI
    /// and a C++/CLI derived class leveraging the unmanaged C++ interop handling C++/CLI provides.
    /// </remarks>
    public ref class ScriptVideoContext sealed : public ScriptVideoContextBase
    {
    private:
        bool _applyMaskingPreviewToSourceRender;

    public:
        /// <summary>
        /// Creates a new instance of the <see cref="ScriptVideoContext"/> class.
        /// </summary>
        /// <remarks>Derived from the <see cref="ScriptVideoContextBase"/> class.</remarks>
        /// <param name="scriptVideoService">The instance of the <see cref="IScriptVideoService"/> for which this class represents the runtime context.</param>
        /// <param name="systemDialogService">An instance of a <see cref="Services::Dialog::ISystemDialogService"/> for forwarding messages to the UI.</param>
        ScriptVideoContext(IScriptVideoService^ scriptVideoService, Services::Dialog::ISystemDialogService^ systemDialogService);

    internal:
        /// <summary>
        /// Gets or sets whether to apply a masking preview to the Direct3D source render surface.
        /// </summary>
        property bool ApplyMaskingPreviewToSourceRender
        {
            bool get();
            void set(bool value);
        }

        /// <summary>
        /// Sets video property values from the fields of an unmanaged <see cref="PreviewRenderer::Unmanaged::LoadedScriptVideoInfo"/> structure.
        /// </summary>
        /// <remarks>
        /// Sets the following properties:
        /// <see cref="HasVideo"/>,
        /// <see cref="VideoFrameSize"/>,
        /// <see cref="VideoFrameCount"/>,
        /// <see cref="SeekableVideoFrameCount"/>,
        /// <see cref="VideoFramerate"/>,
        /// <see cref="VideoDuration"/>,
        /// <see cref="AspectRatio"/>.
        /// </remarks>
        /// <param name="loadedScriptVideoInfo">A reference to an unmanaged <see cref="PreviewRenderer::Unmanaged::LoadedScriptVideoInfo"/> structure containing video property values.</param>
        void SetVideoPropertiesFromUnmanagedStruct(const PreviewRenderer::Unmanaged::LoadedScriptVideoInfo& loadedScriptVideoInfo);

        /// <summary>
        /// Callback method for directly setting the <see cref="ScriptFileSource"/> property backing field value.
        /// </summary>
        /// <remarks>Raises the <see cref="System::ComponentModel::INotifyPropertyChanged::PropertyChanged"/> event for the <see cref="FrameNumber"/> property.</remarks>
        /// <param name="scriptFileSource">The file path of the AviSynth script providing source video to the <see cref="IScriptVideoService"/>.</param>
        void SetScriptFileSourceInternal(System::String^ scriptFileSource);
    };
}

#pragma warning( pop )