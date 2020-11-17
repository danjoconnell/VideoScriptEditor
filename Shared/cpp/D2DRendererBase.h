#pragma once

namespace VideoScriptEditor::Unmanaged
{
    /// <summary>
    /// Base class for Direct2D Rendering
    /// </summary>
    class D2DRendererBase
    {
    protected:
        /* Direct2D drawing components. */

        Microsoft::WRL::ComPtr<ID2D1Factory3> _d2dFactory;
        Microsoft::WRL::ComPtr<ID2D1DeviceContext2> _d2dContext;
        Microsoft::WRL::ComPtr<ID2D1Effect> _gaussianBlurEffect;

        /// <summary>
        /// An <see cref="ID2D1GeometryGroup"/> of combined masking <see cref="ID2D1Geometry"/> objects contained in the <see cref="_maskingGeometriesRef"/> class member
        /// </summary>
        Microsoft::WRL::ComPtr<ID2D1GeometryGroup> _maskingGeometryGroup;

        /* Data References */

        /// <summary>
        /// A reference to a masking geometries <see cref="std::map"/> keyed by masking segment track number
        /// and providing a <see cref="std::pair"/> association between masking segment frame data and <see cref="ID2D1Geometry"/> objects.
        /// </summary>
        /// <seealso cref="VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase"/>
        std::map<int, std::pair<std::shared_ptr<MaskSegmentFrameDataItemBase>, ID2D1GeometryPtr>>& _maskingGeometriesRef;

        /// <summary>
        /// A reference to a cropping segment frame data <see cref="std::map"/> keyed by the cropping segment's track number.
        /// </summary>
        /// <seealso cref="CropSegmentFrameDataItem"/>
        std::map<int, CropSegmentFrameDataItem>& _croppingSegmentFramesRef;

    protected:
        /// <summary>
        /// Base constructor for classes derived from the <see cref="D2DRendererBase"/> class.
        /// </summary>
        /// <param name="maskingGeometries">
        /// A reference to a masking geometries <see cref="std::map"/>, keyed by masking segment track number,
        /// which provides a <see cref="std::pair"/> association between masking segment frame data and <see cref="ID2D1Geometry"/> objects.
        /// </param>
        /// <param name="croppingSegmentFrames">A reference to a cropping segment frame data <see cref="std::map"/> keyed by the cropping segment's track number.</param>
        D2DRendererBase(std::map<int, std::pair<std::shared_ptr<MaskSegmentFrameDataItemBase>, ID2D1GeometryPtr>>& maskingGeometries, std::map<int, CropSegmentFrameDataItem>& croppingSegmentFrames);

    public:
        /// <summary>
        /// Base destructor for classes derived from the <see cref="D2DRendererBase"/> class.
        /// Smart pointers and standard library containers automatically free resources via their destructors.
        /// </summary>
        virtual ~D2DRendererBase() = default;

        /// <summary>
        /// Updates the <see cref="ID2D1Geometry"/> part of the <paramref name="maskingDataGeometryPair"/> using data from its associated <see cref="MaskSegmentFrameDataItemBase"/> data part.
        /// </summary>
        /// <param name="maskingDataGeometryPair">(IN/OUT) A reference to a <see cref="std::pair"/> item which provides an association of masking segment frame data and <see cref="ID2D1Geometry"/> object.</param>
        void UpdateMaskingGeometry(std::pair<std::shared_ptr<MaskSegmentFrameDataItemBase>, ID2D1GeometryPtr>& maskingDataGeometryPair);

        /// <summary>
        /// Updates the masking geometry group by combining the <see cref="ID2D1Geometry"/> objects contained in the <see cref="_maskingGeometriesRef"/> class member
        /// </summary>
        void UpdateMaskingGeometryGroup();

    protected:

        /// <summary>
        /// Configures resources that don't depend on a Direct3D device.
        /// </summary>
        virtual void CreateDeviceIndependentResources();

        /// <summary>
        /// Creates and initializes the Direct2D Gaussian blur effect <see cref="_gaussianBlurEffect"/>.
        /// </summary>
        void CreateGaussianBlurEffect();

        /// <summary>
        /// Creates a compatible render target bitmap for intermediate drawing.
        /// </summary>
        /// <param name="sourceBitmap">(IN) The <see cref="ID2D1Bitmap"/> that <paramref name="sourceCompatibleRenderTargetBitmap"/>'s pixel size and pixel format should match.</param>
        /// <param name="sourceCompatibleRenderTargetBitmap">(OUT) If successful, contains the address of a pointer to a newly created <see cref="ID2D1Bitmap"/>.</param>
        /// <returns>S_OK for success, or failure code</returns>
        HRESULT CreateSourceCompatibleRenderTargetBitmap(const ID2D1Bitmap* sourceBitmap, ID2D1Bitmap1** sourceCompatibleRenderTargetBitmap);

        /// <summary>
        /// Copies the contents of a source <see cref="ID2D1Bitmap1"/> to a destination <see cref="ID2D1Bitmap1"/>.
        /// </summary>
        /// <param name="sourceBitmap">(IN) The source <see cref="ID2D1Bitmap1"/></param>
        /// <param name="destinationBitmap">(OUT) The destination <see cref="ID2D1Bitmap1"/></param>
        /// <returns>S_OK for success, or failure code</returns>
        /// <remarks>
        /// <paramref name="sourceBitmap"/> should/would be declared const, but this method uses <see cref="ID2D1Bitmap::CopyFromBitmap"/>
        /// which doesn't support a const pointer for its bitmap parameter.
        /// </remarks>
        HRESULT CopyD2DBitmap(ID2D1Bitmap1* sourceBitmap, ID2D1Bitmap1* destinationBitmap);

        /// <summary>
        /// Creates a polygon <see cref="ID2D1PathGeometry"/> object using data from a <see cref="MaskPolygonSegmentFrameDataItem"/>
        /// </summary>
        /// <param name="polygonMaskDataItem">(IN) A pointer to a <see cref="MaskPolygonSegmentFrameDataItem"/> containing the data to create the polygon geometry</param>
        /// <param name="pathGeometry">(OUT) If successful, contains the address to a pointer to the created <see cref="ID2D1PathGeometry"/></param>
        /// <returns>S_OK for success, or failure code</returns>
        HRESULT CreatePolygonGeometry(const MaskPolygonSegmentFrameDataItem* polygonMaskDataItem, ID2D1PathGeometry** pathGeometry);

        /// <summary>
        /// Adds a <see cref="ID2D1Geometry"/> to the <paramref name="geometryCollection"/> by combining it with the existing items in the collection.
        /// If the <see cref="ID2D1Geometry"/> can't be combined with the existing items, it is simply added to the collection.
        /// </summary>
        /// <param name="geometry">(IN) A reference to a smart pointer to a <see cref="ID2D1Geometry"/> to combine and add to the collection.</param>
        /// <param name="geometryCollection">(IN/OUT) A reference to the <see cref="std::vector"/> to which the geometry will be combined with and added to.</param>
        void AddCombinedGeometryToCollection(const ID2D1GeometryPtr& geometry, std::vector<Microsoft::WRL::ComPtr<ID2D1Geometry>>& geometryCollection);

        /// <summary>
        /// Renders a blur effect on a frame using a geometric mask defining the areas to blur.
        /// </summary>
        /// <param name="sourceFrameBitmap">(IN) The <see cref="ID2D1Bitmap"/> containing the content to draw and blur.</param>
        /// <param name="renderTargetBitmap">(IN/OUT) The <see cref="ID2D1Bitmap"/> target to render to.</param>
        void RenderBlurMask(ID2D1Bitmap1* sourceFrameBitmap, ID2D1Bitmap1* renderTargetBitmap);

        /// <summary>
        /// Renders a single or multi-segment crop of a source frame <see cref="ID2D1Bitmap"/>.
        /// In the case of a multi-segment crop, the segments are scaled to best fit height and drawn horizontally from left to right.
        /// </summary>
        /// <param name="sourceFrameBitmap">(IN) The source <see cref="ID2D1Bitmap"/> containing the content to crop.</param>
        void RenderCroppedFrameInternal(ID2D1Bitmap* sourceFrameBitmap);

        /// <summary>
        /// Calculates the scaled bounds for rendering a single or multi-segment crop.
        /// A single segment crop is scaled for best fit and centered horizontally and vertically.
        /// For a multi-segment crop, each item is scaled to match the vertically largest item's height,
        /// compositely scaled for best fit for drawing horizontally from left to right and compositely centered horizontally and vertically.
        /// </summary>
        /// <returns>A <see cref="LtwhRectD"/> structure describing the scaled left, top, width and height rectangular bounds.</returns>
        LtwhRectD GetCroppingSegmentFramesRenderBounds();

        /// <summary>
        /// Creates a <see cref="CropSegmentFrameRenderItem"/> structure containing the calculated rendering instructions
        /// such as scale, rotation and translation matrix values for a <see cref="CropSegmentFrameDataItem"/>.
        /// </summary>
        /// <param name="cropSegmentFrameDataItem">(IN) A reference to a <see cref="CropSegmentFrameDataItem"/> containing the cropping data.</param>
        /// <param name="renderBoundingSize">
        /// (IN) A reference to a <see cref="SizeD"/> containing the bounding size for the render target.
        /// Used for calculating scale and translation matrix values.
        /// </param>
        /// <param name="renderOffset">
        /// (IN) A reference to a <see cref="D2D1_POINT_2F"/> containing the vertical and horizontal amount to offset the cropped bitmap when rendering.
        /// Used for calculating translation matrix values.
        /// </param>
        /// <returns>The created <see cref="CropSegmentFrameRenderItem"/> structure.</returns>
        CropSegmentFrameRenderItem CreateCropSegmentFrameRenderItem(const CropSegmentFrameDataItem& cropSegmentFrameDataItem, const SizeD& renderBoundingSize, const D2D1_POINT_2F& renderOffset);
    };
}