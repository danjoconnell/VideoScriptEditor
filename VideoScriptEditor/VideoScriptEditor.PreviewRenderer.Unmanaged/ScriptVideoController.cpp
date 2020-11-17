#include "pch.h"
#include "ScriptVideoController.h"
#include "AviSynthEnvironment.h"
#include "D2DPreviewRenderer.h"
#include <libyuv.h>
#include <stdexcept>
#include <cassert>

namespace VideoScriptEditor::PreviewRenderer::Unmanaged
{
    using namespace VideoScriptEditor::Unmanaged;
    using namespace std;

    ScriptVideoController::ScriptVideoController()
        : _aviSynthEnv(new AviSynthEnvironment()), _renderer(new D2DPreviewRenderer(_maskingPreviewItems, _croppingPreviewItems))
    {
    }

    ScriptVideoController::~ScriptVideoController()
    {
        // Smart pointers and standard library containers automatically free resources via their destructors.
    }

    void ScriptVideoController::ResetEnvironmentAndRenderer()
    {
        _maskingPreviewItems.clear();
        _croppingPreviewItems.clear();

        if (_renderer != nullptr)
        {
            _renderer->ReleaseAndResetResources();
        }

        if (_aviSynthEnv != nullptr)
        {
            _aviSynthEnv->ResetEnvironment();
        }
    }

    LoadedScriptVideoInfo ScriptVideoController::LoadAviSynthScriptFromFile(const string& fileName)
    {
        if (_aviSynthEnv->get_HasLoadedScript())
        {
            _maskingPreviewItems.clear();
            _croppingPreviewItems.clear();
            _renderer->ReleaseAndResetResources();
        }

        LoadedScriptVideoInfo loadedScriptVideoInfo{};

        if (_aviSynthEnv->LoadScriptFromFile(fileName))
        {
            const VideoInfo* vi = _aviSynthEnv->get_VideoInfo();
            assert(vi != nullptr);

            _renderer->InitializeSourceFrameTexture(vi->width, vi->height);

            loadedScriptVideoInfo.HasVideo = vi->HasVideo();
            loadedScriptVideoInfo.PixelWidth = vi->width;
            loadedScriptVideoInfo.PixelHeight = vi->height;
            loadedScriptVideoInfo.FrameCount = vi->num_frames;
            loadedScriptVideoInfo.FpsNumerator = vi->fps_numerator;
            loadedScriptVideoInfo.FpsDenominator = vi->fps_denominator;
        }

        return loadedScriptVideoInfo;
    }

    void ScriptVideoController::InitializePreviewRenderSurface(const VideoSizeInfo& sizeOptions)
    {
        _renderer->InitializePreviewRenderSurface(sizeOptions);
    }

    void ScriptVideoController::SetDirect3D9DeviceWindow(const HWND windowHandle)
    {
        _renderer->SetD3D9DeviceWindow(windowHandle);
    }

    void ScriptVideoController::GetSourceFrameDirect3D9RenderSurface(IDirect3DSurface9Ptr& d3d9SourceFrameSurface)
    {
        _renderer->GetSourceFrameD3D9RenderSurface(d3d9SourceFrameSurface);
    }

    void ScriptVideoController::GetPreviewFrameDirect3D9RenderSurface(IDirect3DSurface9Ptr& d3d9PreviewFrameSurface)
    {
        _renderer->GetPreviewFrameD3D9RenderSurface(d3d9PreviewFrameSurface);
    }

    void ScriptVideoController::RenderSourceFrameSurface(const int frameNumber, const bool applyMaskingPreview)
    {
        CopyFrameToRendererSourceFrameSurface(frameNumber);

        _renderer->RenderSourceFrameSurface(applyMaskingPreview);
    }

    void ScriptVideoController::RenderPreviewFrameSurface(const bool maskingPreviewAppliedToSource)
    {
        _renderer->RenderPreviewFrameSurface(maskingPreviewAppliedToSource);
    }

    void ScriptVideoController::RenderFrameSurfaces(const int frameNumber, const bool applyMaskingPreviewToSource)
    {
        CopyFrameToRendererSourceFrameSurface(frameNumber);

        _renderer->RenderFrameSurfaces(applyMaskingPreviewToSource);
    }

    void ScriptVideoController::UpdateMaskingGeometry(std::pair<std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>, ID2D1GeometryPtr>& maskingDataGeometryPair)
    {
        _renderer->UpdateMaskingGeometry(maskingDataGeometryPair);
    }

    void ScriptVideoController::UpdateMaskingGeometryGroup()
    {
        _renderer->UpdateMaskingGeometryGroup();
    }

    size_t ScriptVideoController::RemoveInactiveMaskingPreviewItems(const std::vector<int>& activePreviewItemKeys)
    {
        return RemoveInactiveSegmentsFromMap(_maskingPreviewItems, activePreviewItemKeys);
    }

    size_t ScriptVideoController::RemoveInactiveCroppingPreviewItems(const std::vector<int>& activePreviewItemKeys)
    {
        return RemoveInactiveSegmentsFromMap(_croppingPreviewItems, activePreviewItemKeys);
    }

    void ScriptVideoController::CopyFrameToRendererSourceFrameSurface(const int frameNumber)
    {
        PVideoFrame sourceVideoFrame;
        try
        {
            sourceVideoFrame = _aviSynthEnv->GetVideoFrame(frameNumber);
        }
        catch (const AvisynthError& aviSynthError)
        {
            // Catching an AvisynthError class exception in managed code would require including the AviSynth.h header in C++/CLI which is problematic.
            // So, we'll do a standard library runtime_error exception re-throw.
            throw std::runtime_error(aviSynthError.msg);
        }

        if (!sourceVideoFrame)
        {
            throw std::invalid_argument("Failed to get the requested video frame from AviSynth.");
        }

        const VideoInfo* vi = _aviSynthEnv->get_VideoInfo();
        assert(vi != nullptr);

        if (!vi->IsYV12())
        {
            throw std::invalid_argument("Video formats other than YV12 are not implemented.");
        }

        // Disable GPU access to the source texture data.
        D3D11_MAPPED_SUBRESOURCE mappedSourceFrameTexture{};
        HR::ThrowIfFailed(
            _renderer->MapSourceFrameTextureForWriting(mappedSourceFrameTexture)
        );

        // Copy the video frame content to the Direct3D source texture
        // by rearranging the (not supported by Direct3D) YV12 U and V planar bytes
        // into (supported by Direct3D) NV12 interleaved UV bytes via libyuv.
        // Lossless conversion since bytes are just being rearranged.

        const uint8_t* srcYPlaneReadPtr = sourceVideoFrame->GetReadPtr(PLANAR_Y);
        const uint8_t* srcUPlaneReadPtr = sourceVideoFrame->GetReadPtr(PLANAR_U);
        const uint8_t* srcVPlaneReadPtr = sourceVideoFrame->GetReadPtr(PLANAR_V);
        const int srcYPlanePitch = sourceVideoFrame->GetPitch(PLANAR_Y);
        const int srcUPlanePitch = sourceVideoFrame->GetPitch(PLANAR_U);
        const int srcVPlanePitch = sourceVideoFrame->GetPitch(PLANAR_V);
        uint8_t* dstWritePtr = static_cast<uint8_t*>(mappedSourceFrameTexture.pData);
        int uvPlaneOffset = (mappedSourceFrameTexture.RowPitch * vi->height);
        uint8_t* dstUVWritePtr = dstWritePtr + uvPlaneOffset;

        int writeNV12Result = libyuv::I420ToNV12(srcYPlaneReadPtr,
                                                 srcYPlanePitch,
                                                 srcUPlaneReadPtr,
                                                 srcUPlanePitch,
                                                 srcVPlaneReadPtr,
                                                 srcVPlanePitch,
                                                 dstWritePtr,
                                                 mappedSourceFrameTexture.RowPitch,
                                                 dstUVWritePtr,
                                                 mappedSourceFrameTexture.RowPitch,
                                                 vi->width,
                                                 vi->height);

        // Re-enable GPU access to the source texture data.
        _renderer->UnmapSourceFrameTexture();

        if (writeNV12Result == -1)
        {
            throw std::runtime_error("Failed to convert YV12 video frame content to NV12 and copy to the Direct3D texture.");
        }

        _renderer->CheckD2DSourceFrameImageSource();
    }
}