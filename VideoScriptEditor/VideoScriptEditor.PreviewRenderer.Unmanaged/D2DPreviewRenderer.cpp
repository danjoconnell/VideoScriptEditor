#include "pch.h"
#include "D2DPreviewRenderer.h"
#include <cassert>

namespace VideoScriptEditor::PreviewRenderer::Unmanaged
{
    using namespace VideoScriptEditor::Unmanaged;
    using Microsoft::WRL::ComPtr;   // See https://github.com/Microsoft/DirectXTK/wiki/ComPtr
    using namespace std;

    D2DPreviewRenderer::D2DPreviewRenderer(std::map<int, std::pair<std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>, ID2D1GeometryPtr>>& maskingGeometries, std::map<int, VideoScriptEditor::Unmanaged::CropSegmentFrameDataItem>& croppingPreviewItems)
        : VideoScriptEditor::Unmanaged::D2DRendererBase(maskingGeometries, croppingPreviewItems),
        _d3dFeatureLevel(D3D_FEATURE_LEVEL_11_0),
        _d3dDriverType(D3D_DRIVER_TYPE_UNKNOWN)
    {
        ZeroMemory(&_sourceFrameRenderTargetDesc, sizeof(_sourceFrameRenderTargetDesc));
        ZeroMemory(&_previewFrameRenderTargetDesc, sizeof(_previewFrameRenderTargetDesc));

        CreateDeviceIndependentResources();

        CreateDeviceResources();
    }

    D2DPreviewRenderer::~D2DPreviewRenderer()
    {
        // Smart pointers and standard library containers automatically free resources via their destructors.
    }

    void D2DPreviewRenderer::SetD3D9DeviceWindow(const HWND windowHandle)
    {
        _d3d9Device.Reset();

        assert(windowHandle != nullptr);

        // Set up the structure used to create the D3D9 Device.
        D3DPRESENT_PARAMETERS d3d9pp{};
        d3d9pp.Windowed = true;
        d3d9pp.SwapEffect = D3DSWAPEFFECT_DISCARD;
        d3d9pp.hDeviceWindow = windowHandle;
        d3d9pp.PresentationInterval = D3DPRESENT_INTERVAL_IMMEDIATE;

        constexpr DWORD behaviorFlags = D3DCREATE_HARDWARE_VERTEXPROCESSING | D3DCREATE_MULTITHREADED | D3DCREATE_FPU_PRESERVE;

        // Create the Direct3D 9 device.
        HR::ThrowIfFailed(
            _d3d9Instance->CreateDeviceEx(D3DADAPTER_DEFAULT,                   // Adapter
                                          D3DDEVTYPE_HAL,                       // DeviceType
                                          windowHandle,                         // hFocusWindow
                                          behaviorFlags,                        // BehaviorFlags
                                          &d3d9pp,                              // pPresentationParameters
                                          nullptr,                              // pFullscreenDisplayMode. Must be NULL for windowed mode.
                                          _d3d9Device.ReleaseAndGetAddressOf()) // ppReturnedDeviceInterface
        );
    }

    void D2DPreviewRenderer::InitializeSourceFrameTexture(const UINT width, const UINT height, const DXGI_FORMAT pixelFormat)
    {
        D3D11_TEXTURE2D_DESC sourceFrameTextureDesc{};
        sourceFrameTextureDesc.Width = width;
        sourceFrameTextureDesc.Height = height;
        sourceFrameTextureDesc.MipLevels = sourceFrameTextureDesc.ArraySize = 1;
        sourceFrameTextureDesc.Format = pixelFormat;
        sourceFrameTextureDesc.SampleDesc.Count = 1;
        sourceFrameTextureDesc.Usage = D3D11_USAGE_DYNAMIC;
        sourceFrameTextureDesc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
        sourceFrameTextureDesc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
        sourceFrameTextureDesc.MiscFlags = 0;

        HR::ThrowIfFailed(
            _d3d11Device->CreateTexture2D(&sourceFrameTextureDesc, nullptr, _sourceFrameTexture.ReleaseAndGetAddressOf())
        );

        // Create and initialize the Direct3D source frame render target texture
        // using the same width, height, etc. as the source frame (input) texture by copying and modifying its D3D11_TEXTURE2D_DESC.
        _sourceFrameRenderTargetDesc = sourceFrameTextureDesc;
        _sourceFrameRenderTargetDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
        _sourceFrameRenderTargetDesc.Usage = D3D11_USAGE_DEFAULT;
        _sourceFrameRenderTargetDesc.BindFlags |= D3D11_BIND_RENDER_TARGET;
        _sourceFrameRenderTargetDesc.CPUAccessFlags = 0;
        _sourceFrameRenderTargetDesc.MiscFlags = D3D11_RESOURCE_MISC_SHARED;

        HR::ThrowIfFailed(
            _d3d11Device->CreateTexture2D(&_sourceFrameRenderTargetDesc, nullptr, _sourceFrameRenderTarget.ReleaseAndGetAddressOf())
        );

        // Create and initialize the source frame render target texture's associated Direct2D render target bitmap.
        HR::ThrowIfFailed(
            InitializeD2DRenderTargetBitmap(_sourceFrameRenderTarget, _sourceFrameRenderTargetDesc.Format, _d2dSourceRenderTargetBitmap.ReleaseAndGetAddressOf())
        );
    }

    void D2DPreviewRenderer::GetSourceFrameD3D9RenderSurface(IDirect3DSurface9Ptr& d3d9SourceFrameSurface)
    {
        GetD3D9SurfaceFromD3D11SharedTexture(_sourceFrameRenderTarget, _sourceFrameRenderTargetDesc, d3d9SourceFrameSurface);
    }

    void D2DPreviewRenderer::GetPreviewFrameD3D9RenderSurface(IDirect3DSurface9Ptr& d3d9PreviewFrameSurface)
    {
        GetD3D9SurfaceFromD3D11SharedTexture(_previewFrameRenderTarget, _previewFrameRenderTargetDesc, d3d9PreviewFrameSurface);
    }

    void D2DPreviewRenderer::InitializePreviewRenderSurface(const VideoSizeInfo& sizeOptions)
    {
        _previewSurfaceSizeOptions = sizeOptions;

        _d2dPreviewRenderTargetBitmap.Reset();
        _previewFrameRenderTarget.Reset();
        ZeroMemory(&_previewFrameRenderTargetDesc, sizeof(_previewFrameRenderTargetDesc));

        _previewFrameRenderTargetDesc.Width = _previewSurfaceSizeOptions.Width;
        _previewFrameRenderTargetDesc.Height = _previewSurfaceSizeOptions.Height;
        _previewFrameRenderTargetDesc.MipLevels = _previewFrameRenderTargetDesc.ArraySize = 1;
        _previewFrameRenderTargetDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
        _previewFrameRenderTargetDesc.SampleDesc.Count = 1;
        _previewFrameRenderTargetDesc.Usage = D3D11_USAGE_DEFAULT;
        _previewFrameRenderTargetDesc.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
        _previewFrameRenderTargetDesc.CPUAccessFlags = 0;
        _previewFrameRenderTargetDesc.MiscFlags = D3D11_RESOURCE_MISC_SHARED;

        HR::ThrowIfFailed(
            _d3d11Device->CreateTexture2D(&_previewFrameRenderTargetDesc, nullptr, _previewFrameRenderTarget.ReleaseAndGetAddressOf())
        );

        HR::ThrowIfFailed(
            InitializeD2DRenderTargetBitmap(_previewFrameRenderTarget, _previewFrameRenderTargetDesc.Format, _d2dPreviewRenderTargetBitmap.ReleaseAndGetAddressOf())
        );
    }

    void D2DPreviewRenderer::RenderSourceFrameSurface(const bool applyMaskingPreview, const bool flushDeviceAfterRender)
    {
        if (applyMaskingPreview && _maskingGeometryGroup != nullptr)
        {
            if (_d2dSourceCompatibleRenderTargetBitmap == nullptr)
            {
                assert(_d2dSourceRenderTargetBitmap != nullptr);

                HR::ThrowIfFailed(
                    CreateSourceCompatibleRenderTargetBitmap(_d2dSourceRenderTargetBitmap.Get(), _d2dSourceCompatibleRenderTargetBitmap.ReleaseAndGetAddressOf())
                );
            }

            _d2dContext->SetTarget(_d2dSourceCompatibleRenderTargetBitmap.Get());
            _d2dContext->BeginDraw();

            // Fill bitmap with a black background.
            _d2dContext->Clear(D2D1::ColorF(D2D1::ColorF::Black, 1.0f));

            // Draw source frame
            _d2dContext->DrawImage(
                _d2dSourceFrameImageSource.Get(),
                nullptr,                                    // default targetOffset of (0,0)
                nullptr,                                    // default imageRectangle (entire image)
                D2D1_INTERPOLATION_MODE_NEAREST_NEIGHBOR,
                D2D1_COMPOSITE_MODE_SOURCE_ATOP
            );

            HR::ThrowIfFailed(
                _d2dContext->EndDraw()
            );

            RenderBlurMask(_d2dSourceCompatibleRenderTargetBitmap.Get(), _d2dSourceRenderTargetBitmap.Get());
        }
        else
        {
            _d2dContext->SetTarget(_d2dSourceRenderTargetBitmap.Get());

            _d2dContext->BeginDraw();

            // Fill bitmap with a black background.
            _d2dContext->Clear(D2D1::ColorF(D2D1::ColorF::Black, 1.0f));

            if (_d2dSourceFrameImageSource != nullptr)
            {
                // Draw source frame
                _d2dContext->DrawImage(
                    _d2dSourceFrameImageSource.Get(),
                    nullptr,                                    // default targetOffset of (0,0)
                    nullptr,                                    // default imageRectangle (entire image)
                    D2D1_INTERPOLATION_MODE_NEAREST_NEIGHBOR,
                    D2D1_COMPOSITE_MODE_SOURCE_ATOP
                );
            }

            HR::ThrowIfFailed(
                _d2dContext->EndDraw()
            );
        }

        if (flushDeviceAfterRender)
        {
            _d3d11DeviceContext->Flush();
        }
    }

    void D2DPreviewRenderer::RenderPreviewFrameSurface(const bool maskingPreviewAppliedToSource, const bool flushDeviceAfterRender)
    {
        bool shouldRenderMaskingPreview = (!maskingPreviewAppliedToSource && _maskingGeometryGroup != nullptr);
        bool shouldRenderCroppingPreview = !_croppingSegmentFramesRef.empty();

        // Retrieve the size of the source render target bitmap.
        D2D1_SIZE_F srcRenderTargetSize = _d2dSourceRenderTargetBitmap->GetSize();

        // Retrieve the size of the preview render target bitmap.
        D2D1_SIZE_F pvwRenderTargetSize = _d2dPreviewRenderTargetBitmap->GetSize();

        D2D1_POINT_2F pvwRenderTargetOffset;
        switch (_previewSurfaceSizeOptions.SizeMode)
        {
        case VideoSizeMode::Letterbox:
            pvwRenderTargetOffset = D2D1::Point2F(
                (pvwRenderTargetSize.width - srcRenderTargetSize.width) / 2.f,
                (pvwRenderTargetSize.height - srcRenderTargetSize.height) / 2.f
            );
            break;
        default: // VideoSizeMode::None
            pvwRenderTargetOffset = D2D1::Point2F();    // default (0,0)
            break;
        }

        if (!shouldRenderMaskingPreview && !shouldRenderCroppingPreview)
        {
            if (_previewSurfaceSizeOptions.SizeMode == VideoSizeMode::None)
            {
                HR::ThrowIfFailed(
                    CopyD2DBitmap(_d2dSourceRenderTargetBitmap.Get(), _d2dPreviewRenderTargetBitmap.Get())
                );
            }
            else
            {
                D2D1_RECT_F destRect = D2D1::RectF(
                    pvwRenderTargetOffset.x,
                    pvwRenderTargetOffset.y,
                    pvwRenderTargetOffset.x + srcRenderTargetSize.width,
                    pvwRenderTargetOffset.y + srcRenderTargetSize.height
                );

                _d2dContext->SetTarget(_d2dPreviewRenderTargetBitmap.Get());

                _d2dContext->BeginDraw();
                _d2dContext->Clear(D2D1::ColorF(D2D1::ColorF::Black, 1.0f));

                _d2dContext->DrawBitmap(
                    _d2dSourceRenderTargetBitmap.Get(),
                    &destRect
                );

                HR::ThrowIfFailed(
                    _d2dContext->EndDraw()
                );
            }
        }
        else
        {
            ID2D1Bitmap1* intermediateTargetBitmap = nullptr;

            if (shouldRenderMaskingPreview)
            {
                if (_d2dSourceCompatibleRenderTargetBitmap == nullptr)
                {
                    assert(_d2dSourceRenderTargetBitmap != nullptr);

                    HR::ThrowIfFailed(
                        CreateSourceCompatibleRenderTargetBitmap(_d2dSourceRenderTargetBitmap.Get(), _d2dSourceCompatibleRenderTargetBitmap.ReleaseAndGetAddressOf())
                    );
                }

                intermediateTargetBitmap = _d2dSourceCompatibleRenderTargetBitmap.Get();

                RenderBlurMask(_d2dSourceRenderTargetBitmap.Get(), intermediateTargetBitmap);

                // Prepare render target for Masking (and possibly Cropping also)
                _d2dContext->SetTarget(_d2dPreviewRenderTargetBitmap.Get());

                if (!shouldRenderCroppingPreview)
                {
                    if (_previewSurfaceSizeOptions.SizeMode == VideoSizeMode::None)
                    {
                        HR::ThrowIfFailed(
                            CopyD2DBitmap(intermediateTargetBitmap, _d2dPreviewRenderTargetBitmap.Get())
                        );
                    }
                    else
                    {
                        // Render to preview texture

                        _d2dContext->BeginDraw();
                        _d2dContext->Clear(D2D1::ColorF(D2D1::ColorF::Black, 1.0f));

                        D2D1_RECT_F destRect = D2D1::RectF(
                            pvwRenderTargetOffset.x,
                            pvwRenderTargetOffset.y,
                            pvwRenderTargetOffset.x + srcRenderTargetSize.width,
                            pvwRenderTargetOffset.y + srcRenderTargetSize.height
                        );

                        _d2dContext->DrawBitmap(
                            intermediateTargetBitmap,
                            &destRect
                        );

                        HR::ThrowIfFailed(
                            _d2dContext->EndDraw()
                        );
                    }
                }
            }
            else
            {
                intermediateTargetBitmap = _d2dSourceRenderTargetBitmap.Get();

                // Prepare render target for Cropping only
                _d2dContext->SetTarget(_d2dPreviewRenderTargetBitmap.Get());
            }

            if (shouldRenderCroppingPreview)
            {
                RenderCroppedFrameInternal(intermediateTargetBitmap);
            }
        }

        if (flushDeviceAfterRender)
        {
            _d3d11DeviceContext->Flush();
        }

        // Clear effect input to ease memory
        _gaussianBlurEffect->SetInput(0, nullptr);
    }

    void D2DPreviewRenderer::RenderFrameSurfaces(const bool applyMaskingPreviewToSource)
    {
        RenderSourceFrameSurface(applyMaskingPreviewToSource, false);

        RenderPreviewFrameSurface(applyMaskingPreviewToSource, false);

        _d3d11DeviceContext->Flush();
    }

    HRESULT D2DPreviewRenderer::MapSourceFrameTextureForWriting(D3D11_MAPPED_SUBRESOURCE& mappedSourceFrameTexture)
    {
        // Disable GPU access to the source texture data.
        return _d3d11DeviceContext->Map(_sourceFrameTexture.Get(),
                                        0,                          // Subresource index
                                        D3D11_MAP_WRITE_DISCARD,
                                        0,                          // Default MapFlags
                                        &mappedSourceFrameTexture);
    }

    void D2DPreviewRenderer::UnmapSourceFrameTexture()
    {
        // Reenable GPU access to the source texture data.
        _d3d11DeviceContext->Unmap(_sourceFrameTexture.Get(),
                                   0);                          // Subresource index
    }

    void D2DPreviewRenderer::CheckD2DSourceFrameImageSource()
    {
        if (_d2dSourceFrameImageSource != nullptr)
        {
            // When not null and bound to a surface, ID2D1ImageSource content is automatically updated
            // so all's good...
            return;
        }

        // Get the DXGI surface to bind to the newly created ID2D1ImageSource
        ComPtr<IDXGISurface> surfaceForD2DImageSource;
        HR::ThrowIfFailed(
            _sourceFrameTexture.As(&surfaceForD2DImageSource)
        );

        // Create the ID2D1ImageSource bound to the DXGI surface
        HR::ThrowIfFailed(
            _d2dContext->CreateImageSourceFromDxgi(
                surfaceForD2DImageSource.GetAddressOf(),
                1,                                              // surfaceCount
                DXGI_COLOR_SPACE_YCBCR_FULL_G22_NONE_P709_X601, // colorSpace
                D2D1_IMAGE_SOURCE_FROM_DXGI_OPTIONS_NONE,       // options
                _d2dSourceFrameImageSource.ReleaseAndGetAddressOf()
            )
        );
    }

    void D2DPreviewRenderer::ReleaseAndResetResources()
    {
        // Finish all pending operations
        _d3d11DeviceContext->Flush();
        _d2dContext->SetTarget(nullptr);

        _maskingGeometryGroup.Reset();

        // Reset Direct2D resources
        _gaussianBlurEffect->SetInput(0, nullptr);
        _d2dSourceCompatibleRenderTargetBitmap.Reset();
        _d2dPreviewRenderTargetBitmap.Reset();
        _d2dSourceRenderTargetBitmap.Reset();
        _d2dSourceFrameImageSource.Reset();

        // Reset Direct3D resources
        _previewFrameRenderTarget.Reset();
        ZeroMemory(&_previewFrameRenderTargetDesc, sizeof(_previewFrameRenderTargetDesc));
        ZeroMemory(&_previewSurfaceSizeOptions, sizeof(_previewSurfaceSizeOptions));
        _sourceFrameRenderTarget.Reset();
        ZeroMemory(&_sourceFrameRenderTargetDesc, sizeof(_sourceFrameRenderTargetDesc));
        _sourceFrameTexture.Reset();
    }

    void D2DPreviewRenderer::CreateDeviceResources()
    {
        // This flag adds support for surfaces with a different color channel ordering
        // than the API default. It is required for compatibility with Direct2D.
        constexpr UINT createDeviceFlags = D3D11_CREATE_DEVICE_BGRA_SUPPORT;

        constexpr D3D_DRIVER_TYPE driverTypes[] =
        {
            D3D_DRIVER_TYPE_HARDWARE,
            D3D_DRIVER_TYPE_WARP
        };
        constexpr UINT numDriverTypes = ARRAYSIZE(driverTypes);

        // DX10 or 11 devices are suitable
        constexpr D3D_FEATURE_LEVEL featureLevels[] =
        {
            D3D_FEATURE_LEVEL_11_1,
            D3D_FEATURE_LEVEL_11_0,
            D3D_FEATURE_LEVEL_10_1,
            D3D_FEATURE_LEVEL_10_0,
        };
        constexpr UINT numFeatureLevels = ARRAYSIZE(featureLevels);

        // Create the Direct3D 11 API device object and a corresponding context.
        ComPtr<ID3D11Device> d3dDevice;
        ComPtr<ID3D11DeviceContext> d3dDeviceContext;

        for (UINT driverTypeIndex = 0; driverTypeIndex < numDriverTypes; ++driverTypeIndex)
        {
            HRESULT hr = D3D11CreateDevice(nullptr,                     // Specify nullptr to use the default adapter.
                                           driverTypes[driverTypeIndex],
                                           nullptr,
                                           createDeviceFlags,           // Set debug and Direct2D compatibility flags.
                                           featureLevels,               // List of feature levels this app can support.
                                           numFeatureLevels,
                                           D3D11_SDK_VERSION,           // Always set this to D3D11_SDK_VERSION for Windows Store apps.
                                           &d3dDevice,                  // Returns the Direct3D device created.
                                           &_d3dFeatureLevel,           // Returns feature level of device created.
                                           &d3dDeviceContext);          // Returns the device immediate context.
            if (SUCCEEDED(hr))
            {
                _d3dDriverType = driverTypes[driverTypeIndex];
                break;
            }
            else if (driverTypeIndex == (numDriverTypes - 1))
            {
                // End of loop and failed to create a D3D11 device - throw exception for HRESULT
                _com_raise_error(hr);
            }
        }

        // Get the Direct3D 11.1 API device and context interfaces.
        HR::ThrowIfFailed(
            d3dDevice.As(&_d3d11Device)
        );
        HR::ThrowIfFailed(
            d3dDeviceContext.As(&_d3d11DeviceContext)
        );

        // Create the Direct2D device object and a corresponding context.
        ComPtr<IDXGIDevice> dxgiDevice;
        HR::ThrowIfFailed(
            _d3d11Device.As(&dxgiDevice)
        );

        HR::ThrowIfFailed(
            _d2dFactory->CreateDevice(dxgiDevice.Get(), _d2dDevice.ReleaseAndGetAddressOf())
        );

        HR::ThrowIfFailed(
            _d2dDevice->CreateDeviceContext(
                D2D1_DEVICE_CONTEXT_OPTIONS_NONE,
                _d2dContext.ReleaseAndGetAddressOf()
            )
        );

        // Create and initialize Direct2D Blur effect
        CreateGaussianBlurEffect();

        // Create the Direct3D 9 Devices for WPF Interop

        // Create the D3D object, which is needed to create the D3D9 Device.
        HR::ThrowIfFailed(
            Direct3DCreate9Ex(D3D_SDK_VERSION, _d3d9Instance.ReleaseAndGetAddressOf())
        );
    }

    HRESULT D2DPreviewRenderer::InitializeD2DRenderTargetBitmap(const Microsoft::WRL::ComPtr<ID3D11Texture2D>& d3dTexture, const DXGI_FORMAT d3dTextureFormat, ID2D1Bitmap1** d2dRenderTargetBitmap)
    {
        //
        // Based on sample code from https://docs.microsoft.com/en-us/windows/win32/direct2d/devices-and-device-contexts#selecting-a-target
        //

        // Direct2D needs the dxgi version of the Direct3D texture surface pointer.
        ComPtr<IDXGISurface> targetSurface;
        HRESULT hr = d3dTexture.As(&targetSurface);
        if (FAILED(hr))
        {
            return hr;
        }

        // Now we set up the Direct2D render target bitmap linked to the shared D3D resource. 
        // Whenever we render to this bitmap, it is directly rendered to the 
        // DXGI texture associated with the shared resource.
        D2D1_BITMAP_PROPERTIES1 bitmapProperties =
            D2D1::BitmapProperties1(
                D2D1_BITMAP_OPTIONS_TARGET,
                D2D1::PixelFormat(
                    d3dTextureFormat,
                    D2D1_ALPHA_MODE_IGNORE
                )
            );

        // Get a D2D surface from the DXGI surface to use as the D2D render target.
        hr = _d2dContext->CreateBitmapFromDxgiSurface(
            targetSurface.Get(),
            &bitmapProperties,
            d2dRenderTargetBitmap
        );

        return hr;
    }

    void D2DPreviewRenderer::GetD3D9SurfaceFromD3D11SharedTexture(const Microsoft::WRL::ComPtr<ID3D11Texture2D>& d3d11Texture, const D3D11_TEXTURE2D_DESC& d3d11TextureDesc, IDirect3DSurface9Ptr& d3d9Surface)
    {
        //
        // Based on sample code from http://jmorrill.hjtcentral.com/Home/tabid/428/EntryId/437/Direct3D-10-11-Direct2D-in-WPF.aspx
        //

        assert(d3d11Texture != nullptr);

        /* The shared handle of the D3D resource */
        HANDLE sharedHandle = nullptr;

        /* Shared texture pulled through the 9Ex device */
        ComPtr<IDirect3DTexture9> pTexture;

        /* Get the shared handle for the given resource */
        ComPtr<IDXGIResource> dxgiResource;
        HR::ThrowIfFailed(
            d3d11Texture.As(&dxgiResource)
        );

        HR::ThrowIfFailed(
            dxgiResource->GetSharedHandle(&sharedHandle)
        );

        D3DFORMAT d3D9Format = DXGIToCrossAPID3D9Format(d3d11TextureDesc.Format);
        assert(d3D9Format != D3DFMT_UNKNOWN);

        /* Get the shared surface.  In this case its really a texture =X */

        /* Create the texture locally, but provide the shared handle.
        * This doesn't really create a new texture, but simply
        * pulls the D3D10/11 resource in the 9Ex device */
        HR::ThrowIfFailed(
            _d3d9Device->CreateTexture(
            d3d11TextureDesc.Width,
            d3d11TextureDesc.Height,
            1,
            D3DUSAGE_RENDERTARGET,
            d3D9Format,
            D3DPOOL_DEFAULT,
            &pTexture,
            &sharedHandle)
        );

        /* Get surface level 0, which we need for the D3DImage */
        HR::ThrowIfFailed(
            pTexture->GetSurfaceLevel(0, &d3d9Surface)
        );
    }

    constexpr D3DFORMAT D2DPreviewRenderer::DXGIToCrossAPID3D9Format(const DXGI_FORMAT dxgiFormat)
    {
        //
        // Based on sample code from http://jmorrill.hjtcentral.com/Home/tabid/428/EntryId/437/Direct3D-10-11-Direct2D-in-WPF.aspx
        //

        switch (dxgiFormat)
        {
        case DXGI_FORMAT_B8G8R8A8_UNORM:
            return D3DFMT_A8R8G8B8;
        case DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
            return D3DFMT_A8R8G8B8;
        case DXGI_FORMAT_B8G8R8X8_UNORM:
            return D3DFMT_X8R8G8B8;
        case DXGI_FORMAT_R8G8B8A8_UNORM:
            return D3DFMT_A8B8G8R8;
        case DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
            return D3DFMT_A8B8G8R8;
        case DXGI_FORMAT_R10G10B10A2_UNORM:
            return D3DFMT_A2B10G10R10;
        case DXGI_FORMAT_R16G16B16A16_FLOAT:
            return D3DFMT_A16B16G16R16F;
        default:
            return D3DFMT_UNKNOWN;
        };
    }
}