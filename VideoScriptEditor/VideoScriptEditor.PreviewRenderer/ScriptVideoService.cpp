#include "pch.h"
#include "ScriptVideoService.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::ComponentModel;
using namespace VideoScriptEditor::Extensions;
using namespace VideoScriptEditor::Models;
using namespace VideoScriptEditor::Models::Primitives;
using namespace VideoScriptEditor::PreviewRenderer;
using System::Drawing::Size;
using System::Diagnostics::Debug;

namespace VideoScriptEditor::Services::ScriptVideo
{
    ScriptVideoService::ScriptVideoService(Services::Dialog::ISystemDialogService^ systemDialogService)
        : ScriptVideoServiceBase(), _internalContext(gcnew ScriptVideoContext(this, systemDialogService))
    {
        try
        {
            _nativeController = new PreviewRenderer::Unmanaged::ScriptVideoController();
        }
        catch (const _com_error& comError)
        {
            throw gcnew PreviewRendererException(comError);
        }
        catch (const std::exception& stdException)
        {
            throw gcnew PreviewRendererException(stdException);
        }
    }

    ScriptVideoService::~ScriptVideoService()
    {
        // Deallocate the native object on a destructor
        delete _nativeController;
    }

    ScriptVideoService::!ScriptVideoService()
    {
        // Deallocate the native object on the finalizer just in case no destructor is called
        delete _nativeController;
    }

    void ScriptVideoService::SetPresentationWindow(System::IntPtr windowHandle)
    {
        try
        {
            _nativeController->SetDirect3D9DeviceWindow(static_cast<HWND>(windowHandle.ToPointer()));
        }
        catch (const _com_error& comError)
        {
            throw gcnew PreviewRendererException(comError);
        }
    }

    void ScriptVideoService::ApplyMaskingPreviewToSourceRender()
    {
        _internalContext->ApplyMaskingPreviewToSourceRender = true;

        RenderUnmanagedSourceFrameSurface();
    }

    void ScriptVideoService::RemoveMaskingPreviewFromSourceRender()
    {
        _internalContext->ApplyMaskingPreviewToSourceRender = false;

        RenderUnmanagedSourceFrameSurface();
    }

    void ScriptVideoService::LoadUnmanagedAviSynthScriptFromFile(String^ scriptFileName)
    {
        std::string scriptFileNameNative;
        MarshalString(scriptFileName, scriptFileNameNative);

        PreviewRenderer::Unmanaged::LoadedScriptVideoInfo loadedScriptVideoInfo = { 0 };
        try
        {
            loadedScriptVideoInfo = _nativeController->LoadAviSynthScriptFromFile(scriptFileNameNative);
        }
        catch (const _com_error& comError)
        {
            throw gcnew PreviewRendererException(comError);
        }
        catch (const std::exception& stdException)
        {
            throw gcnew PreviewRendererException(stdException);
        }

        if (!loadedScriptVideoInfo.HasVideo)
        {
            throw gcnew IO::InvalidDataException(String::Format("AviSynth script '{0}' doesn't output a video", scriptFileName));
        }

        if (_internalContext->ScriptFileSource != scriptFileName)
        {
            _internalContext->SetScriptFileSourceInternal(scriptFileName);
        }

        _internalContext->SetVideoPropertiesFromUnmanagedStruct(loadedScriptVideoInfo);
    }

    void ScriptVideoService::InitializeUnmanagedPreviewRenderSurface()
    {
        VideoSizeOptions outputPreviewSize = _internalContext->OutputPreviewSize;
        PreviewRenderer::Unmanaged::VideoSizeInfo previewVideoSizeOptions;
        previewVideoSizeOptions.SizeMode = VideoResizeModeToUnmanagedVideoSizeMode(outputPreviewSize.ResizeMode);
        previewVideoSizeOptions.Width = outputPreviewSize.PixelWidth;
        previewVideoSizeOptions.Height = outputPreviewSize.PixelHeight;

        try
        {
            _nativeController->InitializePreviewRenderSurface(previewVideoSizeOptions);
        }
        catch (const _com_error& comError)
        {
            throw gcnew PreviewRendererException(comError);
        }
    }

    void ScriptVideoService::PushNewSourceRenderSurfaceToSubscribers()
    {
        IDirect3DSurface9Ptr renderSurfaceComPtr;

        try
        {
            _nativeController->GetSourceFrameDirect3D9RenderSurface(renderSurfaceComPtr);
        }
        catch (const _com_error& comError)
        {
            throw gcnew PreviewRendererException(comError);
        }

        OnNewSourceRenderSurface(IntPtr(renderSurfaceComPtr.GetInterfacePtr()));
    }

    void ScriptVideoService::PushNewPreviewRenderSurfaceToSubscribers()
    {
        IDirect3DSurface9Ptr renderSurfaceComPtr;

        try
        {
            _nativeController->GetPreviewFrameDirect3D9RenderSurface(renderSurfaceComPtr);
        }
        catch (const _com_error& comError)
        {
            throw gcnew PreviewRendererException(comError);
        }

        OnNewPreviewRenderSurface(IntPtr(renderSurfaceComPtr.GetInterfacePtr()));
    }

    void ScriptVideoService::RenderUnmanagedFrameSurfaces(int frameNumber)
    {
        try
        {
            _nativeController->RenderFrameSurfaces(frameNumber, _internalContext->ApplyMaskingPreviewToSourceRender);
        }
        catch (const _com_error& comError)
        {
            throw gcnew PreviewRendererException(GetRenderFrameErrorMessage(frameNumber), comError);
        }
        catch (const std::exception& stdException)
        {
            throw gcnew PreviewRendererException(GetRenderFrameErrorMessage(frameNumber), stdException);
        }
    }

    void ScriptVideoService::RenderUnmanagedPreviewFrameSurface()
    {
        try
        {
            _nativeController->RenderPreviewFrameSurface(_internalContext->ApplyMaskingPreviewToSourceRender);
        }
        catch (const _com_error& comError)
        {
            throw gcnew PreviewRendererException(RenderPreviewFrameErrorMessage, comError);
        }
        catch (const std::exception& stdException)
        {
            throw gcnew PreviewRendererException(RenderPreviewFrameErrorMessage, stdException);
        }

        OnSurfaceRendered(SurfaceRenderPipeline::OutputPreview);
    }

    void ScriptVideoService::SetUnmanagedMaskingPreviewItems(System::Collections::Generic::IEnumerable<SegmentKeyFrameLerpDataItem>^ maskingKeyFrameLerpDataItems)
    {
        auto& unmanagedMaskingPreviewItems = _nativeController->get_MaskingPreviewItems();
        bool geometryGroupNeedsUpdate = false;

        std::vector<int> activeTrackNumbers;

        for each (SegmentKeyFrameLerpDataItem keyFrameLerpDataItem in maskingKeyFrameLerpDataItems)
        {
            activeTrackNumbers.push_back(keyFrameLerpDataItem.TrackNumber);

            // Get existing or insert new item keyed on Track number
            auto& unmanagedMaskPreviewItemPair = unmanagedMaskingPreviewItems[keyFrameLerpDataItem.TrackNumber];
            if (SetUnmanagedMaskDataItemFromLerpedKeyFrames(keyFrameLerpDataItem, unmanagedMaskPreviewItemPair.first))
            {
                // Unmanaged data item was changed

                try
                {
                    _nativeController->UpdateMaskingGeometry(unmanagedMaskPreviewItemPair);
                }
                catch (const _com_error& comError)
                {
                    throw gcnew PreviewRendererException(SetMaskingPreviewItemsErrorMessage, comError);
                }
                catch (const std::exception& stdException)
                {
                    throw gcnew PreviewRendererException(SetMaskingPreviewItemsErrorMessage, stdException);
                }

                geometryGroupNeedsUpdate = true;
            }
        }

        // Remove any excess items not keyed to an active Track number
        if (_nativeController->RemoveInactiveMaskingPreviewItems(activeTrackNumbers) > 0)
        {
            geometryGroupNeedsUpdate = true;
        }

        if (geometryGroupNeedsUpdate)
        {
            try
            {
                _nativeController->UpdateMaskingGeometryGroup();
            }
            catch (const _com_error& comError)
            {
                throw gcnew PreviewRendererException(SetMaskingPreviewItemsErrorMessage, comError);
            }
            catch (const std::exception& stdException)
            {
                throw gcnew PreviewRendererException(SetMaskingPreviewItemsErrorMessage, stdException);
            }
        }
    }

    void ScriptVideoService::SetUnmanagedCroppingPreviewItems(System::Collections::Generic::IEnumerable<SegmentKeyFrameLerpDataItem>^ croppingKeyFrameLerpDataItems)
    {
        using namespace Models::Cropping;

        auto& unmanagedCroppingPreviewItems = _nativeController->get_CroppingPreviewItems();

        std::vector<int> activeTrackNumbers;

        for each (SegmentKeyFrameLerpDataItem keyFrameLerpDataItem in croppingKeyFrameLerpDataItems)
        {
            activeTrackNumbers.push_back(keyFrameLerpDataItem.TrackNumber);

            CropKeyFrameModel^ cropKeyFrameAtOrBefore = dynamic_cast<CropKeyFrameModel^>(keyFrameLerpDataItem.KeyFrameAtOrBefore);
            Debug::Assert(cropKeyFrameAtOrBefore != nullptr);

            double cropLeft, cropTop, cropWidth, cropHeight, cropAngle;

            if (keyFrameLerpDataItem.KeyFrameAfter == nullptr || keyFrameLerpDataItem.LerpAmount == 0.0)
            {
                cropLeft = cropKeyFrameAtOrBefore->Left;
                cropTop = cropKeyFrameAtOrBefore->Top;
                cropWidth = cropKeyFrameAtOrBefore->Width;
                cropHeight = cropKeyFrameAtOrBefore->Height;
                cropAngle = cropKeyFrameAtOrBefore->Angle;
            }
            else
            {
                CropKeyFrameModel^ cropKeyFrameAfter = dynamic_cast<CropKeyFrameModel^>(keyFrameLerpDataItem.KeyFrameAfter);
                Debug::Assert(cropKeyFrameAfter != nullptr);

                cropLeft = MathExtensions::LerpTo(cropKeyFrameAtOrBefore->Left, cropKeyFrameAfter->Left, keyFrameLerpDataItem.LerpAmount);
                cropTop = MathExtensions::LerpTo(cropKeyFrameAtOrBefore->Top, cropKeyFrameAfter->Top, keyFrameLerpDataItem.LerpAmount);
                cropWidth = MathExtensions::LerpTo(cropKeyFrameAtOrBefore->Width, cropKeyFrameAfter->Width, keyFrameLerpDataItem.LerpAmount);
                cropHeight = MathExtensions::LerpTo(cropKeyFrameAtOrBefore->Height, cropKeyFrameAfter->Height, keyFrameLerpDataItem.LerpAmount);
                cropAngle = MathExtensions::LerpTo(cropKeyFrameAtOrBefore->Angle, cropKeyFrameAfter->Angle, keyFrameLerpDataItem.LerpAmount);
            }

            // Get existing or insert new item keyed on Track number
            VideoScriptEditor::Unmanaged::CropSegmentFrameDataItem& unmanagedCropPreviewItem = unmanagedCroppingPreviewItems[keyFrameLerpDataItem.TrackNumber];
            unmanagedCropPreviewItem.Left = cropLeft;
            unmanagedCropPreviewItem.Top = cropTop;
            unmanagedCropPreviewItem.Width = cropWidth;
            unmanagedCropPreviewItem.Height = cropHeight;

            // Round to correct Angle precision rounding errors (e.g. 90 degrees double angle can end up being a tiny fraction above 90.0)
            unmanagedCropPreviewItem.Angle = Math::Round(cropAngle, MathExtensions::FloatingPointPrecision);
        }

        // Remove any excess items not keyed to an active Track number
        _nativeController->RemoveInactiveCroppingPreviewItems(activeTrackNumbers);
    }

    void ScriptVideoService::CloseScriptCore()
    {
        ScriptVideoServiceBase::CloseScriptCore();

        _internalContext->ApplyMaskingPreviewToSourceRender = false;
        _internalContext->SetScriptFileSourceInternal(String::Empty);

        _nativeController->ResetEnvironmentAndRenderer();
    }

    void ScriptVideoService::RenderUnmanagedSourceFrameSurface()
    {
        bool applyMaskingPreview = _internalContext->ApplyMaskingPreviewToSourceRender && _internalContext->HasVideo && _internalContext->Project != nullptr && _internalContext->Project->Masking->Shapes->Count > 0;

        try
        {
            _nativeController->RenderSourceFrameSurface(_internalContext->FrameNumber, applyMaskingPreview);
        }
        catch (const _com_error& comError)
        {
            throw gcnew PreviewRendererException(GetRenderFrameErrorMessage(_internalContext->FrameNumber), comError);
        }
        catch (const std::exception& stdException)
        {
            throw gcnew PreviewRendererException(GetRenderFrameErrorMessage(_internalContext->FrameNumber), stdException);
        }

        OnSurfaceRendered(SurfaceRenderPipeline::SourceVideo);
    }

    bool ScriptVideoService::SetUnmanagedMaskDataItemFromLerpedKeyFrames(SegmentKeyFrameLerpDataItem% managedKeyFrameLerpData, std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>& unmanagedDataItemPtr)
    {
        using namespace Models::Masking::Shapes;

        Debug::Assert(managedKeyFrameLerpData.KeyFrameAtOrBefore != nullptr);

        bool unmanagedDataItemWasSet = false;

        PolygonMaskShapeKeyFrameModel^ fromPolygonMaskShapeFrame = nullptr;
        RectangleMaskShapeKeyFrameModel^ fromRectangleMaskShapeFrame = nullptr;
        EllipseMaskShapeKeyFrameModel^ fromEllipseMaskShapeFrame = nullptr;

        if ((fromPolygonMaskShapeFrame = dynamic_cast<PolygonMaskShapeKeyFrameModel^>(managedKeyFrameLerpData.KeyFrameAtOrBefore)) != nullptr)
        {
            PolygonMaskShapeKeyFrameModel^ toPolygonMaskShapeFrame = dynamic_cast<PolygonMaskShapeKeyFrameModel^>(managedKeyFrameLerpData.KeyFrameAfter);

            std::vector<VideoScriptEditor::Unmanaged::PointD> unmanagedPolygonPoints;
            unmanagedPolygonPoints.reserve(fromPolygonMaskShapeFrame->Points->Count);

            for (int i = 0; i < fromPolygonMaskShapeFrame->Points->Count; i++)
            {
                PointD managedPoint = (toPolygonMaskShapeFrame == nullptr || managedKeyFrameLerpData.LerpAmount == 0.0)
                                       ? fromPolygonMaskShapeFrame->Points[i]
                                       : PointD::Lerp(fromPolygonMaskShapeFrame->Points[i], toPolygonMaskShapeFrame->Points[i], managedKeyFrameLerpData.LerpAmount);

                unmanagedPolygonPoints.emplace_back(managedPoint.X, managedPoint.Y);
            }

            std::shared_ptr<VideoScriptEditor::Unmanaged::MaskPolygonSegmentFrameDataItem> unmanagedPolygonPtr = std::dynamic_pointer_cast<VideoScriptEditor::Unmanaged::MaskPolygonSegmentFrameDataItem>(unmanagedDataItemPtr);
            if (!unmanagedPolygonPtr)
            {
                unmanagedDataItemPtr.reset(new VideoScriptEditor::Unmanaged::MaskPolygonSegmentFrameDataItem(std::move(unmanagedPolygonPoints)));
                unmanagedDataItemWasSet = true;
            }
            else if (unmanagedPolygonPtr->Points != unmanagedPolygonPoints)
            {
                unmanagedPolygonPtr->Points = std::move(unmanagedPolygonPoints);
                unmanagedDataItemWasSet = true;
            }
        }
        else if ((fromRectangleMaskShapeFrame = dynamic_cast<RectangleMaskShapeKeyFrameModel^>(managedKeyFrameLerpData.KeyFrameAtOrBefore)) != nullptr)
        {
            double rectLeft, rectTop, rectWidth, rectHeight;

            RectangleMaskShapeKeyFrameModel^ toRectangleMaskShapeFrame = dynamic_cast<RectangleMaskShapeKeyFrameModel^>(managedKeyFrameLerpData.KeyFrameAfter);
            if (toRectangleMaskShapeFrame == nullptr || managedKeyFrameLerpData.LerpAmount == 0.0)
            {
                rectLeft = fromRectangleMaskShapeFrame->Left;
                rectTop = fromRectangleMaskShapeFrame->Top;
                rectWidth = fromRectangleMaskShapeFrame->Width;
                rectHeight = fromRectangleMaskShapeFrame->Height;
            }
            else
            {
                rectLeft = MathExtensions::LerpTo(fromRectangleMaskShapeFrame->Left, toRectangleMaskShapeFrame->Left, managedKeyFrameLerpData.LerpAmount);
                rectTop = MathExtensions::LerpTo(fromRectangleMaskShapeFrame->Top, toRectangleMaskShapeFrame->Top, managedKeyFrameLerpData.LerpAmount);
                rectWidth = MathExtensions::LerpTo(fromRectangleMaskShapeFrame->Width, toRectangleMaskShapeFrame->Width, managedKeyFrameLerpData.LerpAmount);
                rectHeight = MathExtensions::LerpTo(fromRectangleMaskShapeFrame->Height, toRectangleMaskShapeFrame->Height, managedKeyFrameLerpData.LerpAmount);
            }

            std::shared_ptr<VideoScriptEditor::Unmanaged::MaskRectangleSegmentFrameDataItem> unmanagedRectanglePtr = std::dynamic_pointer_cast<VideoScriptEditor::Unmanaged::MaskRectangleSegmentFrameDataItem>(unmanagedDataItemPtr);
            if (!unmanagedRectanglePtr)
            {
                unmanagedDataItemPtr.reset(new VideoScriptEditor::Unmanaged::MaskRectangleSegmentFrameDataItem(rectLeft,
                                                                                                               rectTop,
                                                                                                               rectWidth,
                                                                                                               rectHeight));
                unmanagedDataItemWasSet = true;
            }
            else if (unmanagedRectanglePtr->Left != rectLeft || unmanagedRectanglePtr->Top != rectTop
                    || unmanagedRectanglePtr->Width != rectWidth || unmanagedRectanglePtr->Height != rectHeight)
            {
                unmanagedRectanglePtr->Left = rectLeft;
                unmanagedRectanglePtr->Top = rectTop;
                unmanagedRectanglePtr->Width = rectWidth;
                unmanagedRectanglePtr->Height = rectHeight;

                unmanagedDataItemWasSet = true;
            }
        }
        else if ((fromEllipseMaskShapeFrame = dynamic_cast<EllipseMaskShapeKeyFrameModel^>(managedKeyFrameLerpData.KeyFrameAtOrBefore)) != nullptr)
        {
            PointD ellipseCenterPoint;
            double ellipseRadiusX, ellipseRadiusY;

            EllipseMaskShapeKeyFrameModel^ toEllipseMaskShapeFrame = dynamic_cast<EllipseMaskShapeKeyFrameModel^>(managedKeyFrameLerpData.KeyFrameAfter);
            if (toEllipseMaskShapeFrame == nullptr || managedKeyFrameLerpData.LerpAmount == 0.0)
            {
                ellipseCenterPoint = fromEllipseMaskShapeFrame->CenterPoint;
                ellipseRadiusX = fromEllipseMaskShapeFrame->RadiusX;
                ellipseRadiusY = fromEllipseMaskShapeFrame->RadiusY;
            }
            else
            {
                ellipseCenterPoint = PointD::Lerp(fromEllipseMaskShapeFrame->CenterPoint, toEllipseMaskShapeFrame->CenterPoint, managedKeyFrameLerpData.LerpAmount);
                ellipseRadiusX = MathExtensions::LerpTo(fromEllipseMaskShapeFrame->RadiusX, toEllipseMaskShapeFrame->RadiusX, managedKeyFrameLerpData.LerpAmount);
                ellipseRadiusY = MathExtensions::LerpTo(fromEllipseMaskShapeFrame->RadiusY, toEllipseMaskShapeFrame->RadiusY, managedKeyFrameLerpData.LerpAmount);
            }

            std::shared_ptr<VideoScriptEditor::Unmanaged::MaskEllipseSegmentFrameDataItem> unmanagedEllipsePtr = std::dynamic_pointer_cast<VideoScriptEditor::Unmanaged::MaskEllipseSegmentFrameDataItem>(unmanagedDataItemPtr);
            if (!unmanagedEllipsePtr)
            {
                unmanagedDataItemPtr.reset(new VideoScriptEditor::Unmanaged::MaskEllipseSegmentFrameDataItem(VideoScriptEditor::Unmanaged::PointD(ellipseCenterPoint.X, ellipseCenterPoint.Y),
                                                                                                             ellipseRadiusX,
                                                                                                             ellipseRadiusY));
                unmanagedDataItemWasSet = true;
            }
            else if (unmanagedEllipsePtr->CenterPoint.X != ellipseCenterPoint.X || unmanagedEllipsePtr->CenterPoint.Y != ellipseCenterPoint.Y
                    || unmanagedEllipsePtr->RadiusX != ellipseRadiusX || unmanagedEllipsePtr->RadiusY != ellipseRadiusY)
            {
                unmanagedEllipsePtr->CenterPoint.X = ellipseCenterPoint.X;
                unmanagedEllipsePtr->CenterPoint.Y = ellipseCenterPoint.Y;
                unmanagedEllipsePtr->RadiusX = ellipseRadiusX;
                unmanagedEllipsePtr->RadiusY = ellipseRadiusY;

                unmanagedDataItemWasSet = true;
            }
        }

        return unmanagedDataItemWasSet;
    }
}