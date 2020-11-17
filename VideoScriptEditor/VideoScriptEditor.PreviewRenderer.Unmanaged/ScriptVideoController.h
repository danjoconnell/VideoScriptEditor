#pragma once

namespace VideoScriptEditor::PreviewRenderer::Unmanaged
{
    // Forward class declarations rather than header includes
    // so that the C++/CLI compiler won't unnecessarily traverse and compile class headers into managed code
    class AviSynthEnvironment;
    class D2DPreviewRenderer;

    /// <summary>
    /// Controller for managing the renderer and script environment
    /// </summary>
    class ScriptVideoController
    {
    private:
        std::unique_ptr<AviSynthEnvironment> _aviSynthEnv;
        std::unique_ptr<D2DPreviewRenderer> _renderer;

        /// <summary>
        /// A masking preview items <see cref="std::map"/> keyed by masking segment track number
        /// which provides a <see cref="std::pair"/> association between masking segment frame data and <see cref="ID2D1Geometry"/> objects.
        /// </summary>
        std::map<int, std::pair<std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>, ID2D1GeometryPtr>> _maskingPreviewItems;

        /// <summary>
        /// A cropping segment preview frame data <see cref="std::map"/> keyed by the cropping segment's track number.
        /// </summary>
        std::map<int, VideoScriptEditor::Unmanaged::CropSegmentFrameDataItem> _croppingPreviewItems;

    public:
        /* Properties */

        /// <summary>
        /// Gets a reference to the masking preview items <see cref="std::map"/> which is keyed by masking segment track number
        /// and provides a <see cref="std::pair"/> association between masking segment frame data and <see cref="ID2D1Geometry"/> objects.
        /// </summary>
        /// <returns>A reference to the masking preview items <see cref="std::map"/>.</returns>
        std::map<int, std::pair<std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>, ID2D1GeometryPtr>>& get_MaskingPreviewItems() { return _maskingPreviewItems; }

        /// <summary>
        /// Gets a reference to the cropping segment preview frame data <see cref="std::map"/> which is keyed by the cropping segment's track number.
        /// </summary>
        /// <returns>A reference to the cropping segment preview frame data <see cref="std::map"/>.</returns>
        std::map<int, VideoScriptEditor::Unmanaged::CropSegmentFrameDataItem>& get_CroppingPreviewItems() { return _croppingPreviewItems; }

    public:
        /// <summary>
        /// Constructor for the <see cref="ScriptVideoController"/> class.
        /// </summary>
        ScriptVideoController();

        /// <summary>
        /// Destructor for the <see cref="ScriptVideoController"/> class.
        /// Smart pointers and standard library containers automatically free resources via their destructors.
        /// </summary>
        ~ScriptVideoController();

        /// <summary>
        /// Finishes pending renderer operations and releases script environment and renderer resources
        /// so that the script environment and renderer are in a reset state.
        /// </summary>
        void ResetEnvironmentAndRenderer();

        /// <summary>
        /// Loads an AviSynth script from a file into the AviSynth environment.
        /// </summary>
        /// <param name="fileName">(IN) A reference to a <see cref="std::string"/> containing the absolute AviSynth script file name.</param>
        /// <returns>A <see cref="LoadedScriptVideoInfo"/> structure containing video information from the loaded script.</returns>
        /// <remarks>Utilizes the AviSynth Import source filter which doesn't support relative file paths.</remarks>
        LoadedScriptVideoInfo LoadAviSynthScriptFromFile(const std::string& fileName);

        /// <summary>
        /// Creates and initializes the Direct3D preview render target texture
        /// and associated Direct2D render target bitmap for a output video size.
        /// </summary>
        /// <param name="sizeOptions">(IN) A reference to a <see cref="VideoSizeInfo"/> structure containing video resizing information.</param>
        void InitializePreviewRenderSurface(const VideoSizeInfo& sizeOptions);

        /// <summary>
        /// Sets the window for presenting the WPF/Direct3D9Ex-compatible shared surface.
        /// </summary>
        /// <param name="windowHandle">(IN) The handle of the window.</param>
        void SetDirect3D9DeviceWindow(const HWND windowHandle);

        /// <summary>
        /// Gets a WPF/Direct3D9Ex-compatible shared surface from the Direct3D11 source frame render target texture.
        /// This is performed using the techniques and sample code at http://jmorrill.hjtcentral.com/Home/tabid/428/EntryId/437/Direct3D-10-11-Direct2D-in-WPF.aspx
        /// </summary>
        /// <param name="d3d9SourceFrameSurface">(OUT) A reference to a smart pointer to a <see cref="IDirect3DSurface9"/> which will contain the shared surface.</param>
        void GetSourceFrameDirect3D9RenderSurface(IDirect3DSurface9Ptr& d3d9SourceFrameSurface);

        /// <summary>
        /// Gets a WPF/Direct3D9Ex-compatible shared surface from the Direct3D11 preview frame render target texture.
        /// This is performed using the techniques and sample code at http://jmorrill.hjtcentral.com/Home/tabid/428/EntryId/437/Direct3D-10-11-Direct2D-in-WPF.aspx
        /// </summary>
        /// <param name="d3d9PreviewFrameSurface">(OUT) A reference to a smart pointer to a <see cref="IDirect3DSurface9"/> which will contain the shared surface.</param>
        void GetPreviewFrameDirect3D9RenderSurface(IDirect3DSurface9Ptr& d3d9PreviewFrameSurface);

        /// <summary>
        /// Renders a source frame and optionally a masking preview frame
        /// to the Direct3D/Direct2D source render target.
        /// </summary>
        /// <param name="frameNumber">(IN) The source frame number to render.</param>
        /// <param name="applyMaskingPreview">(IN) Whether to apply a masking preview frame to the source frame render. Defaults to false.</param>
        void RenderSourceFrameSurface(const int frameNumber, const bool applyMaskingPreview = false);

        /// <summary>
        /// Renders a preview frame to the Direct3D/Direct2D preview render target
        /// using the content of the Direct3D/Direct2D source render target as image source.
        /// </summary>
        /// <param name="maskingPreviewAppliedToSource">(IN) Whether a masking preview frame has been applied to the source frame render target.</param>
        void RenderPreviewFrameSurface(const bool maskingPreviewAppliedToSource);

        /// <summary>
        /// Renders source frame and preview frame Direct3D/Direct2D surfaces,
        /// optionally applying a masking preview frame to the source render.
        /// </summary>
        /// <param name="frameNumber">(IN) The source frame number to render.</param>
        /// <param name="applyMaskingPreviewToSource">(IN) Whether to apply a masking preview frame to the source frame render. Defaults to false.</param>
        void RenderFrameSurfaces(const int frameNumber, const bool applyMaskingPreviewToSource);

        /// <summary>
        /// Updates the <see cref="ID2D1Geometry"/> part of the <paramref name="maskingDataGeometryPair"/> using data from its associated <see cref="VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase"/> data part.
        /// </summary>
        /// <param name="maskingDataGeometryPair">(IN/OUT) A reference to a <see cref="std::pair"/> item which provides an association of masking segment frame data and <see cref="ID2D1Geometry"/> object.</param>
        void UpdateMaskingGeometry(std::pair<std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>, ID2D1GeometryPtr>& maskingDataGeometryPair);

        /// <summary>
        /// Updates the preview renderer's masking geometry group by combining the <see cref="ID2D1Geometry"/> objects contained in the masking preview items collection.
        /// </summary>
        void UpdateMaskingGeometryGroup();

        /// <summary>
        /// Removes all inactive elements from the masking preview items <see cref="std::map"/>.
        /// </summary>
        /// <param name="activePreviewItemKeys">
        /// A <see cref="std::vector"/> of key values to compare with key values of elements in the masking preview items <see cref="std::map"/>.
        /// Any <see cref="std::map"/> element whose key value isn't contained in this collection will be removed.
        /// </param>
        /// <returns>The number of masking preview items that were removed.</returns>
        size_t RemoveInactiveMaskingPreviewItems(const std::vector<int>& activePreviewItemKeys);

        /// <summary>
        /// Removes all inactive elements from the cropping preview items <see cref="std::map"/>.
        /// </summary>
        /// <param name="activePreviewItemKeys">
        /// A <see cref="std::vector"/> of key values to compare with key values of elements in the cropping preview items <see cref="std::map"/>.
        /// Any <see cref="std::map"/> element whose key value isn't contained in this collection will be removed.
        /// </param>
        /// <returns>The number of cropping preview items that were removed.</returns>
        size_t RemoveInactiveCroppingPreviewItems(const std::vector<int>& activePreviewItemKeys);

    private:
        /// <summary>
        /// Copies the content of an AviSynth video frame to the renderer's Direct3D source frame surface.
        /// Performed by rearranging YV12 U and V planar bytes to NV12 interleaved UV bytes via libyuv.
        /// </summary>
        /// <param name="frameNumber">(IN) The frame number of the AviSynth video frame to copy to the renderer's Direct3D source frame surface.</param>
        void CopyFrameToRendererSourceFrameSurface(const int frameNumber);
    };
}
