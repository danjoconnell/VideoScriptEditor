#pragma once
#include "ScriptVideoContext.h"

namespace VideoScriptEditor::Services::ScriptVideo
{
    /// <summary>
    /// A service for processing video from an AviSynth script
    /// and previewing the resulting edited video through a Direct2D renderer.
    /// </summary>
    /// <remarks>
    /// The ScriptVideoService is essentially split up into two parts;
    /// A C# base class containing purely managed code and not practical for coding in C++/CLI
    /// and a C++/CLI derived class leveraging the unmanaged C++ interop handling C++/CLI provides.
    /// </remarks>
    public ref class ScriptVideoService sealed : public ScriptVideoServiceBase
    {
    private:
        ScriptVideoContext^ _internalContext;
        PreviewRenderer::Unmanaged::ScriptVideoController* _nativeController;

    public:
        /// <summary>
        /// Creates a new instance of the <see cref="ScriptVideoService"/> class.
        /// </summary>
        /// <remarks>Derived from the <see cref="ScriptVideoServiceBase"/> class.</remarks>
        /// <param name="systemDialogService">An instance of a <see cref="Services::Dialog::ISystemDialogService"/> for forwarding messages to the UI.</param>
        ScriptVideoService(Services::Dialog::ISystemDialogService^ systemDialogService);

        /// <summary>
        /// Destructor for the <see cref="ScriptVideoService"/> class.
        /// </summary>
        ~ScriptVideoService();

    protected:
        /// <summary>
        /// Finalizer for the <see cref="ScriptVideoService"/> class.
        /// </summary>
        !ScriptVideoService();

        /// <summary>
        /// Gets the internal <see cref="ScriptVideoContextBase">runtime context</see>.
        /// </summary>
        virtual property ScriptVideoContextBase^ InternalContext
        {
            ScriptVideoContextBase^ get() override { return _internalContext; }
        }

    public:
        /// <summary>
        /// Gets a reference to the <see cref="IScriptVideoContext">runtime context</see> of the service.
        /// </summary>
        /// <returns>The <see cref="IScriptVideoContext">runtime context</see> of the service.</returns>
        virtual IScriptVideoContext^ GetContextReference() override { return _internalContext; }

        /// <summary>
        /// Sets the window for presenting the Direct3D source and preview render surfaces.
        /// </summary>
        /// <param name="windowHandle">The handle of the window.</param>
        virtual void SetPresentationWindow(System::IntPtr windowHandle) override;

        /// <summary>
        /// Applies a masking preview to the Direct3D source render surface.
        /// </summary>
        virtual void ApplyMaskingPreviewToSourceRender() override;

        /// <summary>
        /// Removes a masking preview from the Direct3D source render surface.
        /// </summary>
        virtual void RemoveMaskingPreviewFromSourceRender() override;

    protected:
        /// <summary>
        /// Loads an AviSynth script from a file into the unmanaged AviSynth environment
        /// and initializes the unmanaged Direct3D source frame surface via interop.
        /// </summary>
        /// <param name="scriptFileName">The file path of the AviSynth script.</param>
        virtual void LoadUnmanagedAviSynthScriptFromFile(System::String^ scriptFileName) override;

        /// <summary>
        /// Creates and initializes the unmanaged Direct3D preview render surface.
        /// </summary>
        virtual void InitializeUnmanagedPreviewRenderSurface() override;

        /// <summary>
        /// Retrieves a pointer to the unmanaged Direct3D source render surface
        /// and pushes it to subscribers of the <see cref="NewSourceRenderSurface"/> event.
        /// </summary>
        virtual void PushNewSourceRenderSurfaceToSubscribers() override;

        /// <summary>
        /// Retrieves a pointer to the unmanaged Direct3D preview render surface
        /// and pushes it to subscribers of the <see cref="NewPreviewRenderSurface"/> event.
        /// </summary>
        virtual void PushNewPreviewRenderSurfaceToSubscribers() override;

        /// <summary>
        /// Renders unmanaged Direct3D source and preview surfaces for a given frame number.
        /// </summary>
        /// <param name="frameNumber">The source frame number to render.</param>
        virtual void RenderUnmanagedFrameSurfaces(int frameNumber) override;

        /// <summary>
        /// Renders the unmanaged Direct3D preview surface.
        /// </summary>
        virtual void RenderUnmanagedPreviewFrameSurface() override;

        /// <summary>
        /// Sets the content of the unmanaged Direct2D renderer's masking preview items cache via interop to
        /// frame data interpolated from masking segment key frame models.
        /// </summary>
        /// <param name="maskingKeyFrameLerpDataItems">A collection of <see cref="SegmentKeyFrameLerpDataItem">data items</see> for performing masking segment key frame linear interpolation.</param>
        /// <returns>A <see cref="System::Boolean"/> value indicating success or failure.</returns>
        virtual void SetUnmanagedMaskingPreviewItems(System::Collections::Generic::IEnumerable<SegmentKeyFrameLerpDataItem>^ maskingKeyFrameLerpDataItems) override;

        /// <summary>
        /// Sets the content of the unmanaged Direct2D renderer's cropping preview items cache via interop to
        /// frame data interpolated from cropping segment key frame models.
        /// </summary>
        /// <param name="croppingKeyFrameLerpDataItems">A collection of <see cref="SegmentKeyFrameLerpDataItem">data items</see> for performing cropping segment key frame linear interpolation.</param>
        virtual void SetUnmanagedCroppingPreviewItems(System::Collections::Generic::IEnumerable<SegmentKeyFrameLerpDataItem>^ croppingKeyFrameLerpDataItems) override;

        /// <summary>
        /// Core method for finishing pending operations, closing the loaded script,
        /// releasing resources and resetting the runtime context.
        /// </summary>
        virtual void CloseScriptCore() override;

    private:
        /// <summary>
        /// Renders the unmanaged Direct3D source frame surface.
        /// </summary>
        /// <returns>A <see cref="System::Boolean"/> value indicating success or failure.</returns>
        void RenderUnmanagedSourceFrameSurface();

        /// <summary>
        /// Sets the values of an unmanaged mask segment frame data item from frame data interpolated from managed masking segment key frame models
        /// only if the values of the unmanaged data item differ from the interpolated managed frame data.
        /// </summary>
        /// <param name="managedKeyFrameLerpData">A managed tracking reference to a <see cref="SegmentKeyFrameLerpDataItem"/> structure containing masking segment key frame linear interpolation data.</param>
        /// <param name="unmanagedDataItemPtr">A reference to an unmanaged smart pointer to an unmanaged mask segment frame data item.</param>
        /// <returns>True if the values of the unmanaged data item differed from the interpolated managed frame data, False otherwise.</returns>
        bool SetUnmanagedMaskDataItemFromLerpedKeyFrames(SegmentKeyFrameLerpDataItem% managedKeyFrameLerpData, std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>& unmanagedDataItemPtr);

        /* 
            Error handling functions and properties
        */

        /// <summary>
        /// Gets an error message for failing to render a frame.
        /// </summary>
        /// <param name="frameNumber">The zero-based number of the frame that failed to render.</param>
        /// <returns>A <see cref="System::String"/> containing the error message.</returns>
        System::String^ GetRenderFrameErrorMessage(int frameNumber)
        {
            return System::String::Format("Failed to render frame {0}", frameNumber);
        }

        /// <summary>
        /// Gets an error message for failing to render a preview frame.
        /// </summary>
        property System::String^ RenderPreviewFrameErrorMessage
        {
            System::String^ get()
            {
                return System::String::Format("Failed to render preview frame {0}", _internalContext->FrameNumber);
            }
        }

        /// <summary>
        /// Gets an error message for failing to set masking preview items.
        /// </summary>
        property System::String^ SetMaskingPreviewItemsErrorMessage
        {
            System::String^ get()
            {
                return System::String::Format("Failed to set masking items for rendering preview frame {0}", _internalContext->FrameNumber);
            }
        }
    };
}