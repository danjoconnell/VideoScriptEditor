#include "pch.h"
#include "SoftwareD2DRenderer.h"
#include <comdef.h>
#include "..\..\Shared\cpp\ComHelpers.h"

using namespace VideoScriptEditor::Unmanaged;
using Microsoft::WRL::ComPtr;	// See https://github.com/Microsoft/DirectXTK/wiki/ComPtr
using namespace std;

SoftwareD2DRenderer::SoftwareD2DRenderer(const D2D1_SIZE_U& sourceVideoSize, const D2D1_SIZE_U& outputVideoSize, std::map<int, std::pair<std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>, ID2D1GeometryPtr>>& maskingGeometries, std::map<int, VideoScriptEditor::Unmanaged::CropSegmentFrameDataItem>& croppingSegmentFrames)
    : D2DRendererBase(maskingGeometries, croppingSegmentFrames), _sourceVideoSize(sourceVideoSize), _outputVideoSize(outputVideoSize)
{
    CreateDeviceIndependentResources();
}

SoftwareD2DRenderer::~SoftwareD2DRenderer()
{
    // Smart pointers and standard library containers automatically free resources via their destructors.
}

void SoftwareD2DRenderer::RenderBlurMaskedAndCroppedFrame(const PVideoFrame& sourceVideoFrame, PVideoFrame& outputVideoFrame, const VideoInfo& outputVideoFrameInfo)
{
    ComPtr<ID2D1Bitmap1> srcFrameD2DBmp;
    HR::ThrowIfFailed(
        CopyVideoFramePixelsToD2DBitmap(sourceVideoFrame, srcFrameD2DBmp)
    );

    ComPtr<ID2D1Bitmap1> sourceCompatibleRenderTargetBitmap;
    HR::ThrowIfFailed(
        CreateSourceCompatibleRenderTargetBitmap(srcFrameD2DBmp.Get(), sourceCompatibleRenderTargetBitmap.ReleaseAndGetAddressOf())
    );

    // Preserve the pre-existing target.
    ComPtr<ID2D1Image> wicRenderTarget;
    _d2dContext->GetTarget(&wicRenderTarget);

    //
    // Masking
    //

    RenderBlurMask(srcFrameD2DBmp.Get(), sourceCompatibleRenderTargetBitmap.Get());

    // Clear effect input to ease memory
    _gaussianBlurEffect->SetInput(0, nullptr);

    //
    // Cropping
    //

    // Restore render target
    _d2dContext->SetTarget(wicRenderTarget.Get());

    RenderCroppedFrameInternal(sourceCompatibleRenderTargetBitmap.Get());

    CopyRenderTargetBmpPixelsToFrame(outputVideoFrame, outputVideoFrameInfo);
}

void SoftwareD2DRenderer::RenderOverlayMaskFrame(PVideoFrame& outputVideoFrame, const VideoInfo& outputVideoFrameInfo)
{
    ComPtr<ID2D1SolidColorBrush> whiteColorBrush;
    HR::ThrowIfFailed(
        _renderTarget->CreateSolidColorBrush(D2D1::ColorF(D2D1::ColorF::White, 1.0f), &whiteColorBrush)
    );

    _renderTarget->BeginDraw();

    // Fill bitmap with a black background. Shapes will be white.
    _renderTarget->Clear(D2D1::ColorF(D2D1::ColorF::Black, 1.0f));

    _renderTarget->FillGeometry(_maskingGeometryGroup.Get(), whiteColorBrush.Get());
    _renderTarget->DrawGeometry(_maskingGeometryGroup.Get(), whiteColorBrush.Get(), 0.0f);

    HR::ThrowIfFailed(
        _renderTarget->EndDraw()
    );

    CopyRenderTargetBmpPixelsToFrame(outputVideoFrame, outputVideoFrameInfo);
}

void SoftwareD2DRenderer::RenderBlurFrame(const PVideoFrame& sourceVideoFrame, PVideoFrame& outputVideoFrame, const VideoInfo& outputVideoFrameInfo)
{
    ComPtr<ID2D1Bitmap1> srcFrameD2DBmp = nullptr;
    HR::ThrowIfFailed(
        CopyVideoFramePixelsToD2DBitmap(sourceVideoFrame, srcFrameD2DBmp)
    );

    _gaussianBlurEffect->SetInput(0, srcFrameD2DBmp.Get());
    _d2dContext->BeginDraw();
    _d2dContext->Clear(D2D1::ColorF(D2D1::ColorF::Black, 1.f));
    _d2dContext->DrawImage(_gaussianBlurEffect.Get());

    HR::ThrowIfFailed(
        _d2dContext->EndDraw()
    );

    // Clear effect input to ease memory
    _gaussianBlurEffect->SetInput(0, nullptr);

    CopyRenderTargetBmpPixelsToFrame(outputVideoFrame, outputVideoFrameInfo);
}

void SoftwareD2DRenderer::RenderCroppedFrame(const PVideoFrame& sourceVideoFrame, PVideoFrame& outputVideoFrame, const VideoInfo& outputVideoFrameInfo)
{
    ComPtr<ID2D1Bitmap1> srcFrameD2DBmp = nullptr;
    HR::ThrowIfFailed(
        CopyVideoFramePixelsToD2DBitmap(sourceVideoFrame, srcFrameD2DBmp)
    );

    RenderCroppedFrameInternal(srcFrameD2DBmp.Get());

    CopyRenderTargetBmpPixelsToFrame(outputVideoFrame, outputVideoFrameInfo);
}

void SoftwareD2DRenderer::CreateDeviceIndependentResources()
{
    D2DRendererBase::CreateDeviceIndependentResources();

    // Create the COM imaging factory.
    HR::ThrowIfFailed(
        CoCreateInstance(CLSID_WICImagingFactory,
                         nullptr,
                         CLSCTX_INPROC_SERVER,
                         IID_PPV_ARGS(_wicImagingFactory.ReleaseAndGetAddressOf()))
    );

    HR::ThrowIfFailed(
        _wicImagingFactory->CreateBitmap(_outputVideoSize.width,
                                         _outputVideoSize.height,
                                         GUID_WICPixelFormat32bppPBGRA,
                                         WICBitmapCreateCacheOption::WICBitmapCacheOnLoad,
                                         _renderTargetBmp.ReleaseAndGetAddressOf())
    );

    // Set the render target type to D2D1_RENDER_TARGET_TYPE_DEFAULT to use software rendering.
    HR::ThrowIfFailed(
        _d2dFactory->CreateWicBitmapRenderTarget(_renderTargetBmp.Get(),
                                                  D2D1::RenderTargetProperties(),
                                                  _renderTarget.ReleaseAndGetAddressOf())
    );

    HR::ThrowIfFailed(
        _renderTarget.As(&_d2dContext)
    );
    
    CreateGaussianBlurEffect();
}

HRESULT SoftwareD2DRenderer::CopyVideoFramePixelsToD2DBitmap(const PVideoFrame& sourceVideoFrame, Microsoft::WRL::ComPtr<ID2D1Bitmap1>& targetBitmap)
{
    const D2D1_PIXEL_FORMAT renderTargetPixelFormat = _renderTarget->GetPixelFormat();
    const BYTE* srcFrameReadPtr = sourceVideoFrame->GetReadPtr();
    const D2D1_BITMAP_PROPERTIES1 targetBitmapProps = D2D1::BitmapProperties1(D2D1_BITMAP_OPTIONS_NONE, renderTargetPixelFormat);

    return _d2dContext->CreateBitmap(_sourceVideoSize, srcFrameReadPtr, sourceVideoFrame->GetPitch(), &targetBitmapProps, &targetBitmap);
}

void SoftwareD2DRenderer::CopyRenderTargetBmpPixelsToFrame(PVideoFrame& destinationVideoFrame, const VideoInfo& destinationVideoFrameInfo)
{
    WICRect renderTargetBmpLockRect = { 0, 0, destinationVideoFrameInfo.width, destinationVideoFrameInfo.height };
    ComPtr<IWICBitmapLock> renderTargetBmpLock;

    HR::ThrowIfFailed(
        _renderTargetBmp->Lock(&renderTargetBmpLockRect, WICBitmapLockRead, &renderTargetBmpLock)
    );

    UINT renderTargetBmpBufferSize = 0;
    UINT renderTargetBmpBmpStride = 0;
    BYTE* renderTargetBmpReadPtr = nullptr;
    
    HR::ThrowIfFailed(
        renderTargetBmpLock->GetStride(&renderTargetBmpBmpStride)
    );

    HR::ThrowIfFailed(
        renderTargetBmpLock->GetDataPointer(&renderTargetBmpBufferSize, &renderTargetBmpReadPtr)
    );

    const int dstFramePitch = destinationVideoFrame->GetPitch();
    BYTE* dstFrameWritePtr = destinationVideoFrame->GetWritePtr();

    // flip the image vertically during read/write
    if (libyuv::ARGBCopy(renderTargetBmpReadPtr, renderTargetBmpBmpStride, dstFrameWritePtr, dstFramePitch, destinationVideoFrameInfo.width, -destinationVideoFrameInfo.height) == -1)
    {
        throw std::runtime_error("libyuv failed to copy the content of the render target bitmap to the PVideoFrame");
    }
}
