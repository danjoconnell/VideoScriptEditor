#pragma once
#include "..\..\Shared\cpp\D2DRendererBase.h"

namespace VideoScriptEditor::PreviewRenderer::Unmanaged
{
    /// <summary>
    /// Direct2D Preview Renderer.
    /// Derived from the <see cref="VideoScriptEditor::Unmanaged::D2DRendererBase"/> class.
    /// </summary>
    class D2DPreviewRenderer : public VideoScriptEditor::Unmanaged::D2DRendererBase
    {
        // Direct3D objects.
        Microsoft::WRL::ComPtr<ID3D11Device5> _d3d11Device;
        Microsoft::WRL::ComPtr<ID3D11DeviceContext4> _d3d11DeviceContext;
        Microsoft::WRL::ComPtr<IDirect3D9Ex> _d3d9Instance;
        Microsoft::WRL::ComPtr<IDirect3DDevice9Ex> _d3d9Device;

        // Direct3D rendering objects.
        Microsoft::WRL::ComPtr<ID3D11Texture2D> _sourceFrameTexture;
        Microsoft::WRL::ComPtr<ID3D11Texture2D> _sourceFrameRenderTarget;
        Microsoft::WRL::ComPtr<ID3D11Texture2D> _previewFrameRenderTarget;

        // Direct2D drawing components.
        Microsoft::WRL::ComPtr<ID2D1Device2> _d2dDevice;
        Microsoft::WRL::ComPtr<ID2D1Bitmap1> _d2dSourceRenderTargetBitmap;
        Microsoft::WRL::ComPtr<ID2D1ImageSource> _d2dSourceFrameImageSource;
        Microsoft::WRL::ComPtr<ID2D1Bitmap1> _d2dSourceCompatibleRenderTargetBitmap;
        Microsoft::WRL::ComPtr<ID2D1Bitmap1> _d2dPreviewRenderTargetBitmap;

        // Cached device properties.
        D3D_FEATURE_LEVEL _d3dFeatureLevel;
        D3D_DRIVER_TYPE _d3dDriverType;
        D3D11_TEXTURE2D_DESC _sourceFrameRenderTargetDesc;
        D3D11_TEXTURE2D_DESC _previewFrameRenderTargetDesc;

        VideoSizeInfo _previewSurfaceSizeOptions;

    public:
        /// <summary>
        /// Constructor for the <see cref="D2DPreviewRenderer"/> class.
        /// Derived from the <see cref="VideoScriptEditor::Unmanaged::D2DRendererBase"/> class.
        /// </summary>
        /// <param name="maskingGeometries">
        /// A reference to a masking geometries <see cref="std::map"/>, keyed by masking segment track number,
        /// which provides a <see cref="std::pair"/> association between masking segment frame data and <see cref="ID2D1Geometry"/> objects.
        /// </param>
        /// <param name="croppingPreviewItems">A reference to a cropping segment frame data <see cref="std::map"/> keyed by the cropping segment's track number.</param>
        D2DPreviewRenderer(std::map<int, std::pair<std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>, ID2D1GeometryPtr>>& maskingGeometries, std::map<int, VideoScriptEditor::Unmanaged::CropSegmentFrameDataItem>& croppingPreviewItems);

        /// <summary>
        /// Destructor for the <see cref="D2DPreviewRenderer"/> class.
        /// Smart pointers and standard library containers automatically free resources via their destructors.
        /// </summary>
        ~D2DPreviewRenderer();

        /// <summary>
        /// Sets the window for presenting the WPF/Direct3D9Ex-compatible shared surface.
        /// </summary>
        /// <param name="windowHandle">(IN) The handle of the window.</param>
        void SetD3D9DeviceWindow(const HWND windowHandle);

        /// <summary>
        /// Creates and initializes the Direct3D source frame (input) and render target textures
        /// and associated Direct2D render target bitmap.
        /// </summary>
        /// <param name="width">(IN) The texture width (in texels).</param>
        /// <param name="height">(IN) The texture height (in texels).</param>
        /// <param name="pixelFormat">(IN) The source frame (input) texture pixel format. Defaults to <see cref="DXGI_FORMAT::DXGI_FORMAT_NV12"/>.</param>
        void InitializeSourceFrameTexture(const UINT width, const UINT height, const DXGI_FORMAT pixelFormat = DXGI_FORMAT_NV12);

        /// <summary>
        /// Gets a WPF/Direct3D9Ex-compatible shared surface from the <see cref="ID3D11Texture2D"/> source frame render target texture.
        /// This is performed using the techniques and sample code at http://jmorrill.hjtcentral.com/Home/tabid/428/EntryId/437/Direct3D-10-11-Direct2D-in-WPF.aspx
        /// </summary>
        /// <param name="d3d9SourceFrameSurface">(OUT) A reference to a smart pointer to a <see cref="IDirect3DSurface9"/> which will contain the the shared surface.</param>
        void GetSourceFrameD3D9RenderSurface(IDirect3DSurface9Ptr& d3d9SourceFrameSurface);

        /// <summary>
        /// Gets a WPF/Direct3D9Ex-compatible shared surface from the <see cref="ID3D11Texture2D"/> preview frame render target texture.
        /// This is performed using the techniques and sample code at http://jmorrill.hjtcentral.com/Home/tabid/428/EntryId/437/Direct3D-10-11-Direct2D-in-WPF.aspx
        /// </summary>
        /// <param name="d3D9PreviewFrameSurface">(OUT) A reference to a smart pointer to a <see cref="IDirect3DSurface9"/> which will contain the the shared surface.</param>
        void GetPreviewFrameD3D9RenderSurface(IDirect3DSurface9Ptr& d3D9PreviewFrameSurface);

        /// <summary>
        /// Creates and initializes the Direct3D preview render target texture
        /// and associated Direct2D render target bitmap of a specific size.
        /// </summary>
        /// <param name="sizeOptions">(IN) A reference to a <see cref="VideoSizeInfo"/> structure containg the width and height to use.</param>
        void InitializePreviewRenderSurface(const VideoSizeInfo& sizeOptions);

        /// <summary>
        /// Renders a source frame and optionally a masking preview frame
        /// to the Direct3D/Direct2D source render target.
        /// </summary>
        /// <param name="applyMaskingPreview">(IN) Whether to apply a masking preview frame to the source frame render. Defaults to false.</param>
        /// <param name="flushDeviceAfterRender">(IN) Whether to force a Direct3D device flush after the Direct2D drawing has completed. Defaults to true.</param>
        void RenderSourceFrameSurface(const bool applyMaskingPreview = false, const bool flushDeviceAfterRender = true);

        /// <summary>
        /// Renders a preview frame to the Direct3D/Direct2D preview render target
        /// using the content of the Direct3D/Direct2D source render target as image source.
        /// </summary>
        /// <param name="maskingPreviewAppliedToSource">(IN) Whether a masking preview frame has been applied to the source frame render target. Defaults to false.</param>
        /// <param name="flushDeviceAfterRender">(IN) Whether to force a Direct3D device flush after the Direct2D drawing has completed. Defaults to true.</param>
        void RenderPreviewFrameSurface(const bool maskingPreviewAppliedToSource = false, const bool flushDeviceAfterRender = true);

        /// <summary>
        /// Renders source frame and preview frame Direct3D/Direct2D surfaces,
        /// optionally applying a masking preview frame to the source render.
        /// </summary>
        /// <param name="applyMaskingPreviewToSource">(IN) Whether to apply a masking preview frame to the source frame render. Defaults to false.</param>
        void RenderFrameSurfaces(const bool applyMaskingPreviewToSource = false);
        
        /// <summary>
        /// Obtains a CPU write pointer for updating the content of the Direct3D source frame (input) texture
        /// by mapping it for CPU write access and disabling GPU access until <see cref="UnmapSourceFrameTexture"/> is called.
        /// </summary>
        /// <param name="mappedSourceFrameTexture">(IN/OUT) A reference to a <see cref="D3D11_MAPPED_SUBRESOURCE"/> structure which will if successful contain the write pointer to the mapped texture.</param>
        /// <returns>S_OK for success, or failure code</returns>
        HRESULT MapSourceFrameTextureForWriting(D3D11_MAPPED_SUBRESOURCE& mappedSourceFrameTexture);

        /// <summary>
        /// Invalidates the CPU write pointer to the Direct3D source frame (input) texture obtained from calling <see cref="MapSourceFrameTextureForWriting"/>
        /// and reenables GPU access to the texture.
        /// </summary>
        void UnmapSourceFrameTexture();

        /// <summary>
        /// Checks that a valid <see cref="ID2D1ImageSource"/> is present for the Direct3D source frame (input) texture.
        /// If not, this method creates one.
        /// The <see cref="ID2D1ImageSource"/> is used for dynamically converting NV12 (YUV) pixel data from its bound Direct3D texture
        /// into Direct2D compatible RGB pixel data.
        /// </summary>
        void CheckD2DSourceFrameImageSource();

        /// <summary>
        /// Finishes pending operations and releases all Direct3D and Direct2D resources so that the renderer is in a reset state.
        /// Typically this is done when the source frame (input) texture no longer contains valid data to be processed.
        /// </summary>
        void ReleaseAndResetResources();

    private:

        /// <summary>
        /// Configures the Direct3D device, and stores handles to it and the device context.
        /// </summary>
        void CreateDeviceResources();

        /// <summary>
        /// Creates and initializes a Direct2D <see cref="ID2D1Bitmap1"/> from a Direct3D <see cref="ID3D11Texture2D"/> texture that can be rendered to from Direct2D.
        /// </summary>
        /// <param name="d3dTexture">(IN) A reference to a smart pointer to the <see cref="ID3D11Texture2D"/> from which to create the <see cref="ID2D1Bitmap1"/>.</param>
        /// <param name="d3dTextureFormat">(IN) A <see cref="DXGI_FORMAT"/> enum value specifying the size and arrangement of channels in each pixel of the created <see cref="ID2D1Bitmap1"/>.</param>
        /// <param name="d2dRenderTargetBitmap">(OUT) If successful, contains the address of a pointer to a new <see cref="ID2D1Bitmap1"/> that can be used as a Direct2D render target.</param>
        /// <returns>S_OK for success, or failure code</returns>
        HRESULT InitializeD2DRenderTargetBitmap(const Microsoft::WRL::ComPtr<ID3D11Texture2D>& d3dTexture, const DXGI_FORMAT d3dTextureFormat, ID2D1Bitmap1** d2dRenderTargetBitmap);

        /// <summary>
        /// Gets a WPF/Direct3D9Ex-compatible shared surface from a <see cref="ID3D11Texture2D"/> texture.
        /// This is performed using the techniques and sample code at http://jmorrill.hjtcentral.com/Home/tabid/428/EntryId/437/Direct3D-10-11-Direct2D-in-WPF.aspx
        /// </summary>
        /// <param name="d3d11Texture">(IN) A reference to a smart pointer to the <see cref="ID3D11Texture2D"/> from which to obtain the WPF/Direct3D9Ex-compatible shared surface.</param>
        /// <param name="d3d11TextureDesc">(IN) A reference to a <see cref="D3D11_TEXTURE2D_DESC"/> describing width, height and format of the <paramref name="d3d11Texture"/>.</param>
        /// <param name="d3d9Surface">(OUT) A reference to a smart pointer to a <see cref="IDirect3DSurface9"/> which will contain the the shared surface.</param>
        void GetD3D9SurfaceFromD3D11SharedTexture(const Microsoft::WRL::ComPtr<ID3D11Texture2D>& d3d11Texture, const D3D11_TEXTURE2D_DESC& d3d11TextureDesc, IDirect3DSurface9Ptr& d3d9Surface);

        /// <summary>
        /// Converts a cross api shareable D3D10/D3D11 <see cref="DXGI_FORMAT"/> value to its corresponding D3D9 <see cref="D3DFORMAT"/> value.
        /// </summary>
        /// <param name="dxgiFormat">The <see cref="DXGI_FORMAT"/> to convert.</param>
        /// <returns>
        /// The corresponding D3D9 <see cref="D3DFORMAT"/> value 
        /// or <see cref="D3DFMT_UNKNOWN"/> for <see cref="DXGI_FORMAT"/> values that aren't cross api shareable.
        /// </returns>
        constexpr D3DFORMAT DXGIToCrossAPID3D9Format(const DXGI_FORMAT dxgiFormat);
    };
}