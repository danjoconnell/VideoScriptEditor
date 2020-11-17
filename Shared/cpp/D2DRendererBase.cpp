#if !defined(WIN32_LEAN_AND_MEAN)
#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
#endif

#include <windows.h>
#include <d2d1_3.h>
#include <wrl/client.h>
#include <comdef.h>
#include <memory>
#include <map>
#include <vector>

/* Per https://github.com/Microsoft/DirectXTK/wiki/ComPtr,
   while the Windows Runtime C++ Template Library (WRL) ComPtr smart pointer has no runtime dependency on the Windows Runtime
   and therefore can be freely included in regular non-Windows Runtime C++ compiled code (even for Win7 targets),
   it cannot be compiled with the /clr option enabled (conditional compilation error defined in wrl/def.h).
   So, the following define instructs the compiler to use the older _com_ptr_t class template (https://docs.microsoft.com/en-us/cpp/cpp/com-ptr-t-class?view=vs-2019)
   as an alternative COM smart pointer when compiling code/headers that will be externally referenced by /clr compiled code. */
#if defined(CPPCLI_LINKAGE_RESTRICTIONS)
    _COM_SMARTPTR_TYPEDEF(ID2D1Geometry, __uuidof(ID2D1Geometry));
#else
    using ID2D1GeometryPtr = Microsoft::WRL::ComPtr<ID2D1Geometry>;
#endif

#include "ComHelpers.h"
#include "Primitives.h"
#include "CommonDataStructs.h"
#include "D2DRendererBase.h"
#include <cmath>
#include <cassert>

namespace VideoScriptEditor::Unmanaged
{
    using Microsoft::WRL::ComPtr;   // See https://github.com/Microsoft/DirectXTK/wiki/ComPtr
    using namespace std;

    D2DRendererBase::D2DRendererBase(std::map<int, std::pair<std::shared_ptr<MaskSegmentFrameDataItemBase>, ID2D1GeometryPtr>>& maskingGeometries, std::map<int, CropSegmentFrameDataItem>& croppingSegmentFrames)
        : _maskingGeometriesRef(maskingGeometries), _croppingSegmentFramesRef(croppingSegmentFrames)
    {
    }

    void D2DRendererBase::UpdateMaskingGeometry(std::pair<std::shared_ptr<MaskSegmentFrameDataItemBase>, ID2D1GeometryPtr>& maskingDataGeometryPair)
    {
        const std::shared_ptr<MaskSegmentFrameDataItemBase>& maskingFrameDataItem = maskingDataGeometryPair.first;
        ID2D1GeometryPtr& maskingGeometry = maskingDataGeometryPair.second;

        shared_ptr<MaskPolygonSegmentFrameDataItem> polygonDataItem;
        shared_ptr<MaskRectangleSegmentFrameDataItem> rectangleDataItem;
        shared_ptr<MaskEllipseSegmentFrameDataItem> ellipseDataItem;

        if ((polygonDataItem = dynamic_pointer_cast<MaskPolygonSegmentFrameDataItem>(maskingFrameDataItem)) != nullptr)
        {
            ComPtr<ID2D1PathGeometry> polygonGeometry;

            HR::ThrowIfFailed(
                CreatePolygonGeometry(polygonDataItem.get(), polygonGeometry.ReleaseAndGetAddressOf())
            );

            maskingGeometry.Attach(polygonGeometry.Detach());
        }
        else if ((rectangleDataItem = dynamic_pointer_cast<MaskRectangleSegmentFrameDataItem>(maskingFrameDataItem)) != nullptr)
        {
            ID2D1RectangleGeometry* rectangleGeometry;
            
            HR::ThrowIfFailed(
                _d2dFactory->CreateRectangleGeometry(
                    D2D1::RectF(
                        static_cast<FLOAT>(rectangleDataItem->Left),
                        static_cast<FLOAT>(rectangleDataItem->Top),
                        static_cast<FLOAT>(rectangleDataItem->Left + rectangleDataItem->Width),
                        static_cast<FLOAT>(rectangleDataItem->Top + rectangleDataItem->Height)
                    ),
                    &rectangleGeometry
                )
            );

            maskingGeometry.Attach(rectangleGeometry);
        }
        else if ((ellipseDataItem = dynamic_pointer_cast<MaskEllipseSegmentFrameDataItem>(maskingFrameDataItem)) != nullptr)
        {
            ID2D1EllipseGeometry* ellipseGeometry;

            HR::ThrowIfFailed(
                _d2dFactory->CreateEllipseGeometry(
                    D2D1::Ellipse(
                        static_cast<D2D1_POINT_2F>(ellipseDataItem->CenterPoint),
                        static_cast<FLOAT>(ellipseDataItem->RadiusX),
                        static_cast<FLOAT>(ellipseDataItem->RadiusY)
                    ),
                    &ellipseGeometry
                )
            );

            maskingGeometry.Attach(ellipseGeometry);
        }
        else
        {
            _com_raise_error(HRESULT_FROM_WIN32(ERROR_BAD_ARGUMENTS));
        }
    }

    void D2DRendererBase::CreateDeviceIndependentResources()
    {
        // Initialize Direct2D resources.
        D2D1_FACTORY_OPTIONS d2dFactoryOptions{};

#if defined(_DEBUG)
        // If the project is in a debug build, enable Direct2D debugging via SDK Layers.
        d2dFactoryOptions.debugLevel = D2D1_DEBUG_LEVEL_INFORMATION;
#endif
        // Initialize the Direct2D Factory.
        HR::ThrowIfFailed(
            D2D1CreateFactory(D2D1_FACTORY_TYPE_SINGLE_THREADED,
                              __uuidof(ID2D1Factory2),
                              &d2dFactoryOptions,
                              reinterpret_cast<void**>(_d2dFactory.ReleaseAndGetAddressOf()))
        );
    }

    void D2DRendererBase::CreateGaussianBlurEffect()
    {
        HR::ThrowIfFailed(
            _d2dContext->CreateEffect(CLSID_D2D1GaussianBlur, _gaussianBlurEffect.ReleaseAndGetAddressOf())
        );

        HR::ThrowIfFailed(
            _gaussianBlurEffect->SetValue(D2D1_GAUSSIANBLUR_PROP_STANDARD_DEVIATION, 72.0f)
        );

        HR::ThrowIfFailed(
            _gaussianBlurEffect->SetValue(D2D1_GAUSSIANBLUR_PROP_BORDER_MODE, D2D1_BORDER_MODE_HARD)
        );
    }

    HRESULT D2DRendererBase::CreateSourceCompatibleRenderTargetBitmap(const ID2D1Bitmap* sourceBitmap, ID2D1Bitmap1** sourceCompatibleRenderTargetBitmap)
    {
        return _d2dContext->CreateBitmap(sourceBitmap->GetPixelSize(),                      // The pixel size of the bitmap to be created.
                                         nullptr,                                           // No source data will be loaded into the bitmap.
                                         0,                                                 // No source data, so no need specify the pitch.
                                         D2D1::BitmapProperties1(
                                            D2D1_BITMAP_OPTIONS::D2D1_BITMAP_OPTIONS_TARGET,// The bitmap can be used as a device context target.
                                            sourceBitmap->GetPixelFormat()                  // The bitmap's pixel format and alpha mode.
                                         ),
                                         sourceCompatibleRenderTargetBitmap);               // When this method returns, contains the address of a pointer to a new bitmap object.
    }

    HRESULT D2DRendererBase::CopyD2DBitmap(ID2D1Bitmap1* sourceBitmap, ID2D1Bitmap1* destinationBitmap)
    {
        // Copy the entire area of the source bitmap to the destination bitmap
        const D2D1_POINT_2U copyDestPoint = D2D1::Point2U(0U, 0U);
        const D2D1_SIZE_U srcPixelSize = sourceBitmap->GetPixelSize();
        const D2D1_RECT_U copySrcRect = D2D1::RectU(0U, 0U, srcPixelSize.width, srcPixelSize.height);

        return destinationBitmap->CopyFromBitmap(&copyDestPoint,    // The upper-left corner of the area to which the region specified by srcRect is copied.
                                                 sourceBitmap,      // The bitmap to copy from.
                                                 &copySrcRect);     // The area of the source bitmap to copy.
    }

    HRESULT D2DRendererBase::CreatePolygonGeometry(const MaskPolygonSegmentFrameDataItem* polygonMaskDataItem, ID2D1PathGeometry** pathGeometry)
    {
        assert(polygonMaskDataItem->Points.size() > 1);

        HRESULT hr = _d2dFactory->CreatePathGeometry(pathGeometry);
        if (SUCCEEDED(hr))
        {
            ComPtr<ID2D1GeometrySink> geometrySink;
            hr = (*pathGeometry)->Open(&geometrySink);
            if (SUCCEEDED(hr))
            {
                geometrySink->SetFillMode(D2D1_FILL_MODE_WINDING);

                // First point
                D2D1_POINT_2F startPoint = static_cast<D2D1_POINT_2F>(polygonMaskDataItem->Points[0]);
                geometrySink->BeginFigure(
                    startPoint,
                    D2D1_FIGURE_BEGIN_FILLED
                );

                // Remaining points
                size_t polygonPointsCount = polygonMaskDataItem->Points.size();
                vector<D2D1_POINT_2F> linePoints(polygonPointsCount);
                for (size_t i = 1; i < polygonPointsCount; ++i)
                {
                    linePoints[i - 1] = static_cast<D2D1_POINT_2F>(polygonMaskDataItem->Points[i]);
                }

                // Complete the polygon by joining last point back to first
                linePoints[polygonPointsCount - 1] = startPoint;

                geometrySink->AddLines(linePoints.data(), static_cast<UINT32>(linePoints.size()));
                geometrySink->EndFigure(D2D1_FIGURE_END_CLOSED);
            }

            hr = geometrySink->Close();
        }

        return hr;
    }

    void D2DRendererBase::UpdateMaskingGeometryGroup()
    {
        if (_maskingGeometriesRef.empty())
        {
            _maskingGeometryGroup.Reset();
            return;
        }

        vector<ComPtr<ID2D1Geometry>> combinedGeometries;
        for (const auto& maskGeometryTrackPair : _maskingGeometriesRef)
        {
            auto& maskGeometryPair = maskGeometryTrackPair.second;
            AddCombinedGeometryToCollection(maskGeometryPair.second, combinedGeometries);
        }

        vector<ID2D1Geometry*> rawGeometryPtrs(combinedGeometries.size());
        for (size_t i = 0; i < combinedGeometries.size(); ++i)
        {
            rawGeometryPtrs[i] = combinedGeometries[i].Get();
        }

        HR::ThrowIfFailed(
            _d2dFactory->CreateGeometryGroup(
                D2D1_FILL_MODE_WINDING,
                rawGeometryPtrs.data(),
                static_cast<UINT32>(rawGeometryPtrs.size()),
                _maskingGeometryGroup.ReleaseAndGetAddressOf()
            )
        );
    }

    void D2DRendererBase::AddCombinedGeometryToCollection(const ID2D1GeometryPtr& geometry, std::vector<Microsoft::WRL::ComPtr<ID2D1Geometry>>& geometryCollection)
    {
        ComPtr<ID2D1Geometry> insertedOrCombinedGeometry;
        insertedOrCombinedGeometry = geometry;

        if (!geometryCollection.empty())
        {
            D2D1_GEOMETRY_RELATION intersectTestResult;
            for (auto geometryIterator = geometryCollection.begin(); geometryIterator != geometryCollection.end(); )
            {
                HR::ThrowIfFailed(
                    insertedOrCombinedGeometry->CompareWithGeometry(geometryIterator->Get(), nullptr, &intersectTestResult)
                );

                if (intersectTestResult != D2D1_GEOMETRY_RELATION::D2D1_GEOMETRY_RELATION_DISJOINT)
                {
                    // Remove the intersecting geometry from the vector and union combine geometries into a new ID2D1PathGeometry.

                    ComPtr<ID2D1PathGeometry> unionGeometry;
                    HR::ThrowIfFailed(
                        _d2dFactory->CreatePathGeometry(&unionGeometry)
                    );

                    ComPtr<ID2D1GeometrySink> geometrySink;
                    HR::ThrowIfFailed(
                        unionGeometry->Open(&geometrySink)
                    );

                    HR::ThrowIfFailed(
                        insertedOrCombinedGeometry->CombineWithGeometry(
                            geometryIterator->Get(),
                            D2D1_COMBINE_MODE_UNION,
                            nullptr,
                            geometrySink.Get()
                        )
                    );

                    HR::ThrowIfFailed(
                        geometrySink->Close()
                    );

                    insertedOrCombinedGeometry = unionGeometry;

                    geometryIterator = geometryCollection.erase(geometryIterator);
                }
                else
                {
                    geometryIterator++;
                }
            }
        }

        geometryCollection.push_back(std::move(insertedOrCombinedGeometry));
    }

    void D2DRendererBase::RenderBlurMask(ID2D1Bitmap1* sourceFrameBitmap, ID2D1Bitmap1* renderTargetBitmap)
    {
        // Layer 0 (source frame bitmap)
        HR::ThrowIfFailed(
            CopyD2DBitmap(sourceFrameBitmap, renderTargetBitmap)
        );

        _d2dContext->SetTarget(renderTargetBitmap);
        _d2dContext->BeginDraw();

        // Draw layer 1 (blur mask)
        _d2dContext->PushLayer(
            D2D1::LayerParameters(D2D1::InfiniteRect(), _maskingGeometryGroup.Get()),
            nullptr // No need to CreateLayer on Windows 8+
        );

        _gaussianBlurEffect->SetInput(0, sourceFrameBitmap);
        _d2dContext->DrawImage(_gaussianBlurEffect.Get());

        // Flatten layers
        _d2dContext->PopLayer();

        HR::ThrowIfFailed(
            _d2dContext->EndDraw()
        );
    }

    void D2DRendererBase::RenderCroppedFrameInternal(ID2D1Bitmap* sourceFrameBitmap)
    {
        LtwhRectD renderBoundingBox = GetCroppingSegmentFramesRenderBounds();
        SizeD renderBoundingSize(renderBoundingBox.Width, renderBoundingBox.Height);

        const size_t croppingSegmentFramesCount = _croppingSegmentFramesRef.size();
        if (croppingSegmentFramesCount > 1)
        {
            // Multi-segment frame crop

            // Render top-left when targeting the (temporary) segment render bitmap
            D2D1_POINT_2F segmentRenderBitmapRenderOffset = D2D1::Point2F(0.f, 0.f);

            // Determine max required size for bitmap
            D2D1_SIZE_U segmentRenderBitmapSize = D2D1::SizeU(0U, static_cast<UINT32>(ceil(renderBoundingBox.Height)));

            std::vector<CropSegmentFrameRenderItem> cropSegmentFrameRenderItems;
            cropSegmentFrameRenderItems.reserve(croppingSegmentFramesCount);
            for (const auto& croppingTrackDataItemPair : _croppingSegmentFramesRef)
            {
                CropSegmentFrameRenderItem cropSegmentFrameRenderItem = CreateCropSegmentFrameRenderItem(croppingTrackDataItemPair.second, renderBoundingSize, segmentRenderBitmapRenderOffset);

                // Determine max required size for bitmap
                UINT32 ceiledWidth = static_cast<UINT32>(ceil(cropSegmentFrameRenderItem.ScaledSize.width));
                if (ceiledWidth > segmentRenderBitmapSize.width)
                {
                    segmentRenderBitmapSize.width = ceiledWidth;
                }

                cropSegmentFrameRenderItems.push_back(std::move(cropSegmentFrameRenderItem));
            }

            //
            // Perform rendering
            //

            // Preserve the current render target.
            ComPtr<ID2D1Image> previousRenderTarget;
            _d2dContext->GetTarget(&previousRenderTarget);

            ComPtr<ID2D1Bitmap1> segmentRenderBitmap;
            HR::ThrowIfFailed(
                _d2dContext->CreateBitmap(
                    segmentRenderBitmapSize,
                    nullptr,
                    0,
                    D2D1::BitmapProperties1(
                        D2D1_BITMAP_OPTIONS_TARGET,
                        _d2dContext->GetPixelFormat()
                    ),
                    &segmentRenderBitmap
                )
            );

            D2D1_POINT_2F compositeDrawingPos = D2D1::Point2F(
                static_cast<float>(renderBoundingBox.Left),
                static_cast<float>(renderBoundingBox.Top)
            );
            for (auto cropSegmentFrameRenderItem = cropSegmentFrameRenderItems.begin(); cropSegmentFrameRenderItem != cropSegmentFrameRenderItems.end(); ++cropSegmentFrameRenderItem)
            {
                _d2dContext->SetTarget(segmentRenderBitmap.Get());
                _d2dContext->BeginDraw();

                D2D1_MATRIX_3X2_F scaleMatrix = D2D1::Matrix3x2F::Scale(cropSegmentFrameRenderItem->ScaleFactor, cropSegmentFrameRenderItem->ScaleFactor);
                D2D1_MATRIX_3X2_F translationMatrix = D2D1::Matrix3x2F::Translation(cropSegmentFrameRenderItem->TranslationOffsetX, cropSegmentFrameRenderItem->TranslationOffsetY);

                if (abs(cropSegmentFrameRenderItem->RotationAngle) != 0.f)
                {
                    D2D1_MATRIX_3X2_F rotationMatrix = D2D1::Matrix3x2F::Rotation(cropSegmentFrameRenderItem->RotationAngle, cropSegmentFrameRenderItem->RotationCenter);
                    _d2dContext->SetTransform(rotationMatrix * scaleMatrix * translationMatrix);
                }
                else
                {
                    _d2dContext->SetTransform(scaleMatrix * translationMatrix);
                }

                _d2dContext->DrawBitmap(sourceFrameBitmap);

                // Reset Transform to default
                _d2dContext->SetTransform(D2D1::Matrix3x2F::Identity());

                HR::ThrowIfFailed(
                    _d2dContext->EndDraw()
                );

                // Draw composite image
                _d2dContext->SetTarget(previousRenderTarget.Get());
                _d2dContext->BeginDraw();

                if (cropSegmentFrameRenderItem == cropSegmentFrameRenderItems.begin())
                {
                    // Black-fill render target bitmap background before drawing first item
                    _d2dContext->Clear(D2D1::ColorF(D2D1::ColorF::Black, 1.0f));
                }

                _d2dContext->DrawBitmap(
                    segmentRenderBitmap.Get(),
                    D2D1::RectF(
                        compositeDrawingPos.x,
                        compositeDrawingPos.y,
                        compositeDrawingPos.x + cropSegmentFrameRenderItem->ScaledSize.width,
                        compositeDrawingPos.y + cropSegmentFrameRenderItem->ScaledSize.height
                    ),
                    1.f,
                    D2D1_INTERPOLATION_MODE_LINEAR,
                    D2D1::RectF(
                        0.f,
                        0.f,
                        cropSegmentFrameRenderItem->ScaledSize.width,
                        cropSegmentFrameRenderItem->ScaledSize.height
                    )
                );

                HR::ThrowIfFailed(
                    _d2dContext->EndDraw()
                );

                compositeDrawingPos.x += cropSegmentFrameRenderItem->ScaledSize.width;
            }
        }
        else
        {
            // Single-segment frame crop

            // Retrieve the size of the render target.
            D2D1_SIZE_F renderTargetSize = _d2dContext->GetSize();

            // Set the render offset to center horizontally and vertically.
            D2D1_POINT_2F renderOffset = D2D1::Point2F(
                static_cast<float>((renderTargetSize.width - renderBoundingBox.Width) / 2.0),
                static_cast<float>((renderTargetSize.height - renderBoundingBox.Height) / 2.0)
            );

            CropSegmentFrameRenderItem cropSegmentFrameRenderItem = CreateCropSegmentFrameRenderItem(_croppingSegmentFramesRef.begin()->second, renderBoundingSize, renderOffset);

            D2D1_RECT_F clipBoundsRect = D2D1::RectF(
                static_cast<float>(renderBoundingBox.Left),
                static_cast<float>(renderBoundingBox.Top),
                static_cast<float>(renderBoundingBox.Left + renderBoundingBox.Width),
                static_cast<float>(renderBoundingBox.Top + renderBoundingBox.Height)
            );

            //
            // Perform rendering
            //

            _d2dContext->BeginDraw();
            _d2dContext->Clear(D2D1::ColorF(D2D1::ColorF::Black, 1.0f));

            _d2dContext->PushAxisAlignedClip(&clipBoundsRect, D2D1_ANTIALIAS_MODE_PER_PRIMITIVE);

            D2D1_MATRIX_3X2_F scaleMatrix = D2D1::Matrix3x2F::Scale(cropSegmentFrameRenderItem.ScaleFactor, cropSegmentFrameRenderItem.ScaleFactor);
            D2D1_MATRIX_3X2_F translationMatrix = D2D1::Matrix3x2F::Translation(cropSegmentFrameRenderItem.TranslationOffsetX, cropSegmentFrameRenderItem.TranslationOffsetY);

            if (abs(cropSegmentFrameRenderItem.RotationAngle) != 0.f)
            {
                D2D1_MATRIX_3X2_F rotationMatrix = D2D1::Matrix3x2F::Rotation(cropSegmentFrameRenderItem.RotationAngle, cropSegmentFrameRenderItem.RotationCenter);
                _d2dContext->SetTransform(rotationMatrix * scaleMatrix * translationMatrix);
            }
            else
            {
                _d2dContext->SetTransform(scaleMatrix * translationMatrix);
            }

            _d2dContext->DrawBitmap(sourceFrameBitmap);

            _d2dContext->PopAxisAlignedClip();

            // Reset Transform to default
            _d2dContext->SetTransform(D2D1::Matrix3x2F::Identity());

            HR::ThrowIfFailed(
                _d2dContext->EndDraw()
            );
        }
    }

    LtwhRectD D2DRendererBase::GetCroppingSegmentFramesRenderBounds()
    {
        //
        // Calculate composite size
        //
        SizeD compositeSize{};
        if (_croppingSegmentFramesRef.size() > 1)
        {
            // Multi-segment frame crop

            // Find max height
            for (const auto& croppingTrackDataItemPair : _croppingSegmentFramesRef)
            {
                const CropSegmentFrameDataItem& croppingDataItem = croppingTrackDataItemPair.second;

                if (croppingDataItem.Height > compositeSize.Height)
                {
                    compositeSize.Height = croppingDataItem.Height;
                }
            }

            // Scale (to max height) and combine widths
            for (const auto& croppingTrackDataItemPair : _croppingSegmentFramesRef)
            {
                const CropSegmentFrameDataItem& croppingDataItem = croppingTrackDataItemPair.second;

                double itemWidth = (croppingDataItem.Height != compositeSize.Height)
                                    ? (croppingDataItem.Width * compositeSize.Height) / croppingDataItem.Height // Scale item width to composite height
                                    : croppingDataItem.Width;

                compositeSize.Width += itemWidth;
            }
        }
        else
        {
            // Single-segment frame crop

            const CropSegmentFrameDataItem& croppingDataItem = _croppingSegmentFramesRef.begin()->second;
            compositeSize.Width = croppingDataItem.Width;
            compositeSize.Height = croppingDataItem.Height;
        }

        //
        // Scale composite size
        // Adapted from sample code at https://selbie.wordpress.com/2011/01/23/scale-crop-and-center-an-image-with-correct-aspect-ratio-in-html-and-javascript/
        //

        LtwhRectD compositeRenderBounds{};

        // Retrieve the size of the render target.
        D2D1_SIZE_F renderTargetSize = _d2dContext->GetSize();

        // scale to the target width
        double scaleWidthX = renderTargetSize.width;
        double scaleWidthY = (compositeSize.Height * renderTargetSize.width) / compositeSize.Width;

        // scale to the target height
        double scaleHeightX = (compositeSize.Width * renderTargetSize.height) / compositeSize.Height;
        double scaleHeightY = renderTargetSize.height;

        // now figure out which one we should use
        bool scaleToTargetWidth = (scaleHeightX > renderTargetSize.width);

        if (scaleToTargetWidth)
        {
            compositeRenderBounds.Width = scaleWidthX;
            compositeRenderBounds.Height = scaleWidthY;

            compositeRenderBounds.Top = (renderTargetSize.height - compositeRenderBounds.Height) / 2.0;
        }
        else
        {
            compositeRenderBounds.Width = scaleHeightX;
            compositeRenderBounds.Height = scaleHeightY;

            compositeRenderBounds.Left = (renderTargetSize.width - compositeRenderBounds.Width) / 2.0;
        }

        return compositeRenderBounds;
    }

    CropSegmentFrameRenderItem D2DRendererBase::CreateCropSegmentFrameRenderItem(const CropSegmentFrameDataItem& cropSegmentFrameDataItem, const SizeD& renderBoundingSize, const D2D1_POINT_2F& renderOffset)
    {
        CropSegmentFrameRenderItem cropSegmentFrameRenderItem{};

        // Calculate Rotation values
        float cropAngle = static_cast<float>(cropSegmentFrameDataItem.Angle);
        if (abs(cropAngle) != 0.f) // abs since cropAngle can be -0.0 for zero rotation angle
        {
            cropSegmentFrameRenderItem.RotationAngle = -cropAngle;
            cropSegmentFrameRenderItem.RotationCenter = D2D1::Point2F(
                static_cast<float>((cropSegmentFrameDataItem.Left + (cropSegmentFrameDataItem.Left + cropSegmentFrameDataItem.Width)) / 2.0),
                static_cast<float>((cropSegmentFrameDataItem.Top + (cropSegmentFrameDataItem.Top + cropSegmentFrameDataItem.Height)) / 2.0)
            );
        }

        // Scale to Height
        double scaleFactor = renderBoundingSize.Height / cropSegmentFrameDataItem.Height;
        cropSegmentFrameRenderItem.ScaleFactor = static_cast<float>(scaleFactor);
        cropSegmentFrameRenderItem.ScaledSize = D2D1::SizeF(
            static_cast<float>((cropSegmentFrameDataItem.Width * renderBoundingSize.Height) / cropSegmentFrameDataItem.Height),
            static_cast<float>(renderBoundingSize.Height)
        );

        // Calculate Translation values
        cropSegmentFrameRenderItem.TranslationOffsetX = static_cast<float>(-((cropSegmentFrameDataItem.Left * scaleFactor) - renderOffset.x));
        cropSegmentFrameRenderItem.TranslationOffsetY = static_cast<float>(-((cropSegmentFrameDataItem.Top * scaleFactor) - renderOffset.y));

        return cropSegmentFrameRenderItem;
    }
}