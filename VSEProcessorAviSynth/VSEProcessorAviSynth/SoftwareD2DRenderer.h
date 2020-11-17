#pragma once
#include "..\..\Shared\cpp\D2DRendererBase.h"

/// <summary>
/// Software Direct2D Renderer.
/// Derived from the <see cref="VideoScriptEditor::Unmanaged::D2DRendererBase"/> class.
/// </summary>
class SoftwareD2DRenderer : public VideoScriptEditor::Unmanaged::D2DRendererBase
{
    const D2D1_SIZE_U _sourceVideoSize;
    const D2D1_SIZE_U _outputVideoSize;

    // Windows Imaging Component (WIC) objects.
    Microsoft::WRL::ComPtr<IWICImagingFactory> _wicImagingFactory;
    Microsoft::WRL::ComPtr<IWICBitmap> _renderTargetBmp;

    // Direct2D objects.
    Microsoft::WRL::ComPtr<ID2D1RenderTarget> _renderTarget;

public:
    /// <summary>
    /// Constructor for the <see cref="SoftwareD2DRenderer"/> class.
    /// Derived from the <see cref="VideoScriptEditor::Unmanaged::D2DRendererBase"/> class.
    /// </summary>
    /// <param name="sourceVideoSize">
    /// A reference to a <see cref="D2D1_SIZE_U"/> structure containing the width and height of the source video in pixels.
    /// </param>
    /// <param name="outputVideoSize">
    /// A reference to a <see cref="D2D1_SIZE_U"/> structure containing the width and height of the output video in pixels.
    /// </param>
    /// <param name="maskingGeometries">
    /// A reference to a masking geometries <see cref="std::map"/>, keyed by masking segment track number,
    /// which provides a <see cref="std::pair"/> association between masking segment frame data and <see cref="ID2D1Geometry"/> objects.
    /// </param>
    /// <param name="croppingSegmentFrames">A reference to a cropping segment frame data <see cref="std::map"/> keyed by the cropping segment's track number.</param>
    SoftwareD2DRenderer(const D2D1_SIZE_U& sourceVideoSize, const D2D1_SIZE_U& outputVideoSize, std::map<int, std::pair<std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>, ID2D1GeometryPtr>>& maskingGeometries, std::map<int, VideoScriptEditor::Unmanaged::CropSegmentFrameDataItem>& croppingSegmentFrames);

    /// <summary>
    /// Destructor for the <see cref="SoftwareD2DRenderer"/> class.
    /// Smart pointers and standard library containers automatically free resources via their destructors.
    /// </summary>
    ~SoftwareD2DRenderer();

    /// <summary>
    /// Renders a blur mask effect and cropped <paramref name="sourceVideoFrame"/>
    /// to the <paramref name="outputVideoFrame"/>.
    /// </summary>
    /// <param name="sourceVideoFrame">(IN) A reference to the source <see cref="PVideoFrame"/>.</param>
    /// <param name="outputVideoFrame">(IN/OUT) A reference to the output <see cref="PVideoFrame"/>.</param>
    /// <param name="outputVideoFrameInfo">
    /// (IN) A reference to a <see cref="VideoInfo"/> structure containing the width and height of the <paramref name="outputVideoFrame"/>.
    /// </param>
    void RenderBlurMaskedAndCroppedFrame(const PVideoFrame& sourceVideoFrame, PVideoFrame& outputVideoFrame, const VideoInfo& outputVideoFrameInfo);

    /// <summary>
    /// Renders a black and white geometric mask to the <paramref name="outputVideoFrame"/>
    /// for use as an AviSynth Overlay filter mask.
    /// </summary>
    /// <param name="outputVideoFrame">(IN/OUT) A reference to the output <see cref="PVideoFrame"/>.</param>
    /// <param name="outputVideoFrameInfo">
    /// (IN) A reference to a <see cref="VideoInfo"/> structure containing the width and height of the <paramref name="outputVideoFrame"/>.
    /// </param>
    void RenderOverlayMaskFrame(PVideoFrame& outputVideoFrame, const VideoInfo& outputVideoFrameInfo);

    /// <summary>
    /// Renders a blurred <paramref name="sourceVideoFrame"/> to the <paramref name="outputVideoFrame"/>.
    /// </summary>
    /// <param name="sourceVideoFrame">(IN) A reference to the source <see cref="PVideoFrame"/>.</param>
    /// <param name="outputVideoFrame">(IN/OUT) A reference to the output <see cref="PVideoFrame"/>.</param>
    /// <param name="outputVideoFrameInfo">
    /// (IN) A reference to a <see cref="VideoInfo"/> structure containing the width and height of the <paramref name="outputVideoFrame"/>.
    /// </param>
    void RenderBlurFrame(const PVideoFrame& sourceVideoFrame, PVideoFrame& outputVideoFrame, const VideoInfo& outputVideoFrameInfo);

    /// <summary>
    /// Renders a cropped <paramref name="sourceVideoFrame"/> to the <paramref name="outputVideoFrame"/>.
    /// </summary>
    /// <param name="sourceVideoFrame">(IN) A reference to the source <see cref="PVideoFrame"/>.</param>
    /// <param name="outputVideoFrame">(IN/OUT) A reference to the output <see cref="PVideoFrame"/>.</param>
    /// <param name="outputVideoFrameInfo">
    /// (IN) A reference to a <see cref="VideoInfo"/> structure containing the width and height of the <paramref name="outputVideoFrame"/>.
    /// </param>
    void RenderCroppedFrame(const PVideoFrame& sourceVideoFrame, PVideoFrame& outputVideoFrame, const VideoInfo& outputVideoFrameInfo);

protected:
    /// <summary>
    /// Configures resources that don't depend on a Direct3D device.
    /// </summary>
    virtual void CreateDeviceIndependentResources() override;

private:
    /// <summary>
    /// Copies the content of the <paramref name="sourceVideoFrame"/> to the Direct2D <paramref name="targetBitmap"/>.
    /// </summary>
    /// <param name="sourceVideoFrame">(IN) A reference to the source <see cref="PVideoFrame"/>.</param>
    /// <param name="targetBitmap">(IN/OUT) A smart pointer reference to the Direct2D <paramref name="targetBitmap"/>.</param>
    /// <returns>S_OK for success, or failure code</returns>
    HRESULT CopyVideoFramePixelsToD2DBitmap(const PVideoFrame& sourceVideoFrame, Microsoft::WRL::ComPtr<ID2D1Bitmap1>& targetBitmap);

    /// <summary>
    /// Copies the content of the <see cref="_renderTargetBmp"/> to the <paramref name="destinationVideoFrame"/>.
    /// </summary>
    /// <param name="destinationVideoFrame">(IN/OUT) A reference to the destination <see cref="PVideoFrame"/>.</param>
    /// <param name="destinationVideoFrameInfo">
    /// (IN) A reference to a <see cref="VideoInfo"/> structure containing the width and height of the <paramref name="destinationVideoFrame"/>.
    /// </param>
    void CopyRenderTargetBmpPixelsToFrame(PVideoFrame& destinationVideoFrame, const VideoInfo& destinationVideoFrameInfo);
};