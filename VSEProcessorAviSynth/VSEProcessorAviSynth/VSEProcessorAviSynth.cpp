#include "pch.h"
#include "VSEProcessorAviSynth.h"
#include "VSEProjectFileParser.h"
#include "SingleFrameClip.h"

using namespace VideoScriptEditor::Unmanaged;
using Microsoft::WRL::ComPtr;   // See https://github.com/Microsoft/DirectXTK/wiki/ComPtr
using namespace std;

VSEProcessorAviSynth::VSEProcessorAviSynth(PClip childClip, const char* projectFileName, IScriptEnvironment* env)
    : GenericVideoFilter(childClip)
{
    {
        VSEProjectFileParser projectFileParser(_project);
        projectFileParser.Parse(projectFileName);
    }

    _sourceClip = child;

    VideoProcessingOptionsModel& videoProcessingOptions = _project.VideoProcessingOptions;
    if (videoProcessingOptions.OutputVideoResizeMode == VideoResizeMode::LetterboxToAspectRatio || videoProcessingOptions.OutputVideoResizeMode == VideoResizeMode::LetterboxToSize)
    {
        if (videoProcessingOptions.OutputVideoResizeMode == VideoResizeMode::LetterboxToAspectRatio)
        {
            videoProcessingOptions.OutputVideoSize = MathHelpers::ExpandToAspectRatio(
                D2D1::SizeU(vi.width, vi.height),
                videoProcessingOptions.OutputAspectRatio
            );
        }

        int totalBorderX = videoProcessingOptions.OutputVideoSize.width - vi.width;
        int totalBorderY = videoProcessingOptions.OutputVideoSize.height - vi.height;
        assert(totalBorderX % 2 == 0 && totalBorderY % 2 == 0);

        if (totalBorderX > 0 && totalBorderY > 0)
        {
            env->ThrowError(PLUGIN_NAME ": Can't letterbox both width and height");
        }

        int borderLeftRight = totalBorderX / 2;
        int borderTopBottom = totalBorderY / 2;

        if (borderLeftRight % YV12_MOD_FACTOR > 0 || borderTopBottom % YV12_MOD_FACTOR > 0)
        {
            // Overlay
            const char* blankClipArgNames[] = { "clip", "width", "height", "audio_rate" };
            AVSValue blankClipArgVals[] = { child, static_cast<int>(videoProcessingOptions.OutputVideoSize.width), static_cast<int>(videoProcessingOptions.OutputVideoSize.height), 0 };
            PClip backgroundClip = InvokeAvsFilter(env, "BlankClip", AVSValue(blankClipArgVals, ARRAYSIZE(blankClipArgVals)), blankClipArgNames);
            child = InvokeAvsOverlayFilter(env, backgroundClip, child, borderLeftRight, borderTopBottom);
        }
        else
        {
            // AddBorders
            AVSValue addBordersArgVals[] = { child, borderLeftRight, borderTopBottom, borderLeftRight, borderTopBottom };
            child = InvokeAvsFilter(env, "AddBorders", AVSValue(addBordersArgVals, ARRAYSIZE(addBordersArgVals)));
        }

        vi = child->GetVideoInfo();
    }
    else
    {
        videoProcessingOptions.OutputVideoSize = D2D1::SizeU(vi.width, vi.height);
    }

    if (_project.NeedsDirect2DProcessing)
    {
        VideoInfo sourceClipVideoInfo = _sourceClip->GetVideoInfo();

        _d2dRenderer = make_unique<SoftwareD2DRenderer>(D2D1::SizeU(sourceClipVideoInfo.width, sourceClipVideoInfo.height), D2D1::SizeU(vi.width, vi.height), _activeMaskingSegments, _activeCroppingSegments);

        _d2dRgbSourceClip = InvokeAvsColorConversionFilter(env, "ConvertToRGB32", _sourceClip);
        _d2dRgbSourceClip = InvokeAvsFilter(env, "FlipVertical", _d2dRgbSourceClip);
    }
    else
    {
        _d2dRenderer = nullptr;
        _d2dRgbSourceClip = nullptr;
    }
}

PVideoFrame __stdcall VSEProcessorAviSynth::GetFrame(int n, IScriptEnvironment* env)
{
    if (!_activeMaskingSegmentTracks.empty())
    {
        _activeMaskingSegmentTracks.clear();
    }

    if (!_activeCroppingSegmentTracks.empty())
    {
        _activeCroppingSegmentTracks.clear();
    }

    bool maskingGeometryGroupNeedsUpdate = false;

    // Has to be a linear search unfortunately as a binary search on SegmentModel.StartFrame misses valid matches
    // due to the collection not being able to be sorted for every possible '(n >= SegmentModel.StartFrame && n <= SegmentModel.EndFrame)'.
    for (const SegmentModel& segmentModel : _project.SegmentModels)
    {
        if (n >= segmentModel.StartFrame && n <= segmentModel.EndFrame)
        {
            // A binary search on KeyFrameModelBase.FrameNumber works though :)
            auto keyFrameAtOrAfterIter = segmentModel.KeyFrames.lower_bound(n);

            if (keyFrameAtOrAfterIter == segmentModel.KeyFrames.end())
            {
                assert(keyFrameAtOrAfterIter != segmentModel.KeyFrames.begin());
                --keyFrameAtOrAfterIter;
            }

            std::shared_ptr<KeyFrameModelBase> keyFrameAtOrAfter = keyFrameAtOrAfterIter->second;
            std::shared_ptr<KeyFrameModelBase> keyFrameBefore;
            double lerpAmount = 0.0;

            if (keyFrameAtOrAfterIter->first > n)
            {
                // Frame n isn't a key frame.
                // Get keyFrameBefore (keyFrameAtOrAfterIter - 1) and Lerp from keyFrameBefore to keyFrameAtOrAfter.
                assert(keyFrameAtOrAfterIter != segmentModel.KeyFrames.begin());
                keyFrameBefore = std::prev(keyFrameAtOrAfterIter)->second;

                int frameRange = keyFrameAtOrAfter->FrameNumber - keyFrameBefore->FrameNumber;
                assert(frameRange > 0);

                lerpAmount = (static_cast<double>(n) - static_cast<double>(keyFrameBefore->FrameNumber)) / frameRange;
            }

            if (segmentModel.Type == SegmentType::Crop)
            {
                auto cropSegmentKeyFrameAtOrAfter = dynamic_pointer_cast<CropKeyFrameModel>(keyFrameAtOrAfter);
                auto cropSegmentKeyFrameAtOrBefore = keyFrameBefore != nullptr ? dynamic_pointer_cast<CropKeyFrameModel>(keyFrameBefore) : cropSegmentKeyFrameAtOrAfter;
                assert(cropSegmentKeyFrameAtOrAfter != nullptr && cropSegmentKeyFrameAtOrBefore != nullptr);

                _activeCroppingSegmentTracks.push_back(segmentModel.TrackNumber);

                // Get existing or insert new item keyed on Track number
                CropSegmentFrameDataItem& cropSegmentFrame = _activeCroppingSegments[segmentModel.TrackNumber];
                cropSegmentKeyFrameAtOrBefore->SetFrameDataItemFromLerpedKeyFrames(cropSegmentKeyFrameAtOrAfter, lerpAmount, cropSegmentFrame);
            }
            else  // SegmentType::Mask[Shape]
            {
                auto maskSegmentKeyFrameAtOrAfter = dynamic_pointer_cast<MaskKeyFrameModelBase>(keyFrameAtOrAfter);
                auto maskSegmentKeyFrameAtOrBefore = keyFrameBefore != nullptr ? dynamic_pointer_cast<MaskKeyFrameModelBase>(keyFrameBefore) : maskSegmentKeyFrameAtOrAfter;
                assert(maskSegmentKeyFrameAtOrAfter != nullptr && maskSegmentKeyFrameAtOrBefore != nullptr);

                _activeMaskingSegmentTracks.push_back(segmentModel.TrackNumber);

                // Get existing or insert new item keyed on Track number
                auto& maskingFrameItemPair = _activeMaskingSegments[segmentModel.TrackNumber];
                if (maskSegmentKeyFrameAtOrBefore->SetFrameDataItemFromLerpedKeyFrames(maskSegmentKeyFrameAtOrAfter, lerpAmount, maskingFrameItemPair.first))
                {
                    // Frame data item was changed
                    assert(_d2dRenderer != nullptr);

                    _d2dRenderer->UpdateMaskingGeometry(maskingFrameItemPair);
                    maskingGeometryGroupNeedsUpdate = true;
                }
            }
        }
    }

    // Remove items not keyed to an active Track number
    RemoveInactiveSegmentsFromMap(_activeCroppingSegments, _activeCroppingSegmentTracks);
    if (RemoveInactiveSegmentsFromMap(_activeMaskingSegments, _activeMaskingSegmentTracks) > 0)
    {
        maskingGeometryGroupNeedsUpdate = true;
    }

    if (maskingGeometryGroupNeedsUpdate)
    {
        assert(_d2dRenderer != nullptr);

        _d2dRenderer->UpdateMaskingGeometryGroup();
    }

    if (_activeMaskingSegments.empty() && _activeCroppingSegments.empty())
    {
        return child->GetFrame(n, env);
    }
    else if (!_activeMaskingSegments.empty() && !_activeCroppingSegments.empty() && (_activeCroppingSegments.size() > 1 || abs(static_cast<float>(_activeCroppingSegments.begin()->second.Angle)) != 0.f))
    {
        // All-in-one Direct2D mask and crop
        return ProcessActiveSegmentsUsingDirect2D(n, env);
    }
    else
    {
        PClip processedClip = child;
        const VideoInfo sourceClipVideoInfo = _sourceClip->GetVideoInfo();
        POINT sourceClipOffset = {
            (vi.width - sourceClipVideoInfo.width) / 2,
            (vi.height - sourceClipVideoInfo.height) / 2
        };

        if (!_activeMaskingSegments.empty())
        {
            PClip maskingOverlaySourceClip = (_activeCroppingSegments.empty() || abs(static_cast<float>(_activeCroppingSegments.begin()->second.Angle)) == 0.f) ? child : _sourceClip;
            processedClip = ApplyBlurMask(sourceClipOffset, maskingOverlaySourceClip, n, env);
        }

        if (!_activeCroppingSegments.empty())
        {
            // Perform crop(s)

            if (_activeCroppingSegments.size() > 1 || abs(static_cast<float>(_activeCroppingSegments.begin()->second.Angle)) != 0.f)
            {
                assert(_activeMaskingSegments.empty());

                return ProcessActiveSegmentsUsingDirect2D(n, env);
            }
            else
            {
                return ApplySingleAxisAlignedCrop(processedClip, _activeCroppingSegments.begin()->second, sourceClipOffset, n, env);
            }
        }

        // Masking frame
        return processedClip->GetFrame(n, env);
    }
}

PClip VSEProcessorAviSynth::ApplyBlurMask(const POINT& maskGeometryOffset, const PClip& overlaySourceClip, const int frameNumber, IScriptEnvironment* env)
{
    VideoInfo maskFramesInfo = _sourceClip->GetVideoInfo();
    maskFramesInfo.pixel_type = VideoInfo::CS_BGR32;
    maskFramesInfo.num_frames = 1;

    PVideoFrame maskFrame = env->NewVideoFrame(maskFramesInfo);
    _d2dRenderer->RenderOverlayMaskFrame(maskFrame, maskFramesInfo);

    PVideoFrame blurFrame = env->NewVideoFrame(maskFramesInfo);
    _d2dRenderer->RenderBlurFrame(_d2dRgbSourceClip->GetFrame(frameNumber, env), blurFrame, maskFramesInfo);

    PClip maskClip = new SingleFrameClip(maskFramesInfo, maskFrame);
    PClip blurClip = new SingleFrameClip(maskFramesInfo, blurFrame);

    return InvokeAvsOverlayFilter(env, overlaySourceClip, blurClip, static_cast<int>(maskGeometryOffset.x), static_cast<int>(maskGeometryOffset.y), maskClip);
}

PVideoFrame VSEProcessorAviSynth::ProcessActiveSegmentsUsingDirect2D(const int frameNumber, IScriptEnvironment* env)
{
    VideoInfo processedFrameInfo = vi;
    processedFrameInfo.pixel_type = VideoInfo::CS_BGR32;
    processedFrameInfo.num_frames = 1;

    PVideoFrame processedFrame = env->NewVideoFrame(processedFrameInfo);

    PVideoFrame rgbSourceFrame = _d2dRgbSourceClip->GetFrame(frameNumber, env);
    
    if (!_activeMaskingSegments.empty())
    {
        _d2dRenderer->RenderBlurMaskedAndCroppedFrame(rgbSourceFrame, processedFrame, processedFrameInfo);
    }
    else
    {
        _d2dRenderer->RenderCroppedFrame(rgbSourceFrame, processedFrame, processedFrameInfo);
    }

    PClip processedClip = new SingleFrameClip(processedFrameInfo, processedFrame);
    processedClip = InvokeAvsColorConversionFilter(env, "ConvertToYV12", processedClip);
    return processedClip->GetFrame(frameNumber, env);
}

PVideoFrame VSEProcessorAviSynth::ApplySingleAxisAlignedCrop(const PClip& croppingSourceClip, const CropSegmentFrameDataItem& cropSegmentFrameData, const POINT& cropSegmentFrameOffset, const int frameNumber, IScriptEnvironment* env)
{
    assert(croppingSourceClip->GetVideoInfo().width == vi.width && croppingSourceClip->GetVideoInfo().height == vi.height);

    SingleAxisAlignedCropRenderData cropRenderData = CalculateRenderDataForSingleAxisAlignedCrop(cropSegmentFrameData, cropSegmentFrameOffset, env);

    AVSValue resizeArgs[] = { croppingSourceClip, vi.width, vi.height, cropRenderData.SourceLeft, cropRenderData.SourceTop, cropRenderData.SourceWidth, cropRenderData.SourceHeight };
    PClip processedClip = InvokeAvsFilter(env, "Spline64Resize", AVSValue(resizeArgs, ARRAYSIZE(resizeArgs)));

    if (cropRenderData.BorderLeftRight > 0 || cropRenderData.BorderTopBottom > 0)
    {
        // Fill-in borders
        if (cropRenderData.BorderLeftRight % YV12_MOD_FACTOR == 0 && cropRenderData.BorderTopBottom % YV12_MOD_FACTOR == 0)
        {
            PVideoFrame croppedFrame = processedClip->GetFrame(frameNumber, env);
            PVideoFrame borderedFrame = croppedFrame;
            if (!env->MakeWritable(&borderedFrame))
            {
                env->ThrowError(PLUGIN_NAME ": Failed to make frame writable.");
            }
            
            FillYV12Borders(borderedFrame, vi, cropRenderData.BorderLeftRight, cropRenderData.BorderTopBottom, env);

            return borderedFrame;
        }
        else
        {
            // Overlay borders
            processedClip = OverlayBorders(processedClip, cropRenderData.BorderLeftRight, cropRenderData.BorderTopBottom, env);
        }
    }

    return processedClip->GetFrame(frameNumber, env);
}

SingleAxisAlignedCropRenderData VSEProcessorAviSynth::CalculateRenderDataForSingleAxisAlignedCrop(const CropSegmentFrameDataItem& cropSegmentFrameData, const POINT& cropSegmentFrameOffset, IScriptEnvironment* env)
{
    SingleAxisAlignedCropRenderData calculatedRenderData = { 0 };

    int totalBorderX = 0, totalBorderY = 0;
    if (cropSegmentFrameData.Width != static_cast<double>(vi.width) && cropSegmentFrameData.Height != static_cast<double>(vi.height))
    {
        // Calculate Scale values

        // scale to the target width
        double scaleWidthX = static_cast<double>(vi.width);
        double scaleWidthY = (cropSegmentFrameData.Height * vi.width) / cropSegmentFrameData.Width;

        // scale to the target height
        double scaleHeightX = (cropSegmentFrameData.Width * vi.height) / cropSegmentFrameData.Height;
        double scaleHeightY = static_cast<double>(vi.height);

        // now figure out which one we should use
        bool scaleToTargetWidth = (scaleHeightX > static_cast<double>(vi.width));

        SizeD scaleDimensions{};
        double scaleFactor;
        double arConvRatio;

        if (scaleToTargetWidth)
        {
            arConvRatio = static_cast<double>(vi.height) / static_cast<double>(vi.width);
            scaleDimensions.Width = scaleWidthX;
            scaleDimensions.Height = scaleWidthY;
            scaleFactor = scaleDimensions.Width / cropSegmentFrameData.Width;

            double sourceHeight = cropSegmentFrameData.Width * arConvRatio;
            calculatedRenderData.SourceWidth = static_cast<float>(cropSegmentFrameData.Width);
            calculatedRenderData.SourceLeft = static_cast<float>(cropSegmentFrameData.Left + cropSegmentFrameOffset.x);
            calculatedRenderData.SourceHeight = static_cast<float>(sourceHeight);
            calculatedRenderData.SourceTop = static_cast<float>((cropSegmentFrameData.Top + cropSegmentFrameOffset.y) - ((sourceHeight - cropSegmentFrameData.Height) / 2.0));

            totalBorderY = MathHelpers::RoundToNearestEvenIntegral(static_cast<double>(vi.height) - scaleDimensions.Height);
            calculatedRenderData.BorderTopBottom = totalBorderY / 2;
        }
        else
        {
            arConvRatio = static_cast<double>(vi.width) / static_cast<double>(vi.height);
            scaleDimensions.Width = scaleHeightX;
            scaleDimensions.Height = scaleHeightY;
            scaleFactor = scaleDimensions.Height / cropSegmentFrameData.Height;

            double sourceWidth = cropSegmentFrameData.Height * arConvRatio;
            calculatedRenderData.SourceHeight = static_cast<float>(cropSegmentFrameData.Height);
            calculatedRenderData.SourceTop = static_cast<float>(cropSegmentFrameData.Top + cropSegmentFrameOffset.y);
            calculatedRenderData.SourceWidth = static_cast<float>(sourceWidth);
            calculatedRenderData.SourceLeft = static_cast<float>((cropSegmentFrameData.Left + cropSegmentFrameOffset.x) - ((sourceWidth - cropSegmentFrameData.Width) / 2.0));

            totalBorderX = MathHelpers::RoundToNearestEvenIntegral(static_cast<double>(vi.width) - scaleDimensions.Width);
            calculatedRenderData.BorderLeftRight = totalBorderX / 2;
        }
    }
    else if (cropSegmentFrameData.Width == static_cast<double>(vi.width))
    {
        calculatedRenderData.SourceWidth = static_cast<float>(vi.width);
        calculatedRenderData.SourceLeft = static_cast<float>(cropSegmentFrameData.Left + cropSegmentFrameOffset.x);
        calculatedRenderData.SourceHeight = static_cast<float>(vi.height);
        calculatedRenderData.SourceTop = static_cast<float>((cropSegmentFrameData.Top + cropSegmentFrameOffset.y) - ((static_cast<double>(vi.height) - cropSegmentFrameData.Height) / 2.0));

        totalBorderY = MathHelpers::RoundToNearestEvenIntegral(static_cast<double>(vi.height) - cropSegmentFrameData.Height);
        calculatedRenderData.BorderTopBottom = totalBorderY / 2;
    }
    else if (cropSegmentFrameData.Height == static_cast<double>(vi.height))
    {
        calculatedRenderData.SourceHeight = static_cast<float>(vi.height);
        calculatedRenderData.SourceTop = static_cast<float>(cropSegmentFrameData.Top + cropSegmentFrameOffset.y);
        calculatedRenderData.SourceWidth = static_cast<float>(vi.width);
        calculatedRenderData.SourceLeft = static_cast<float>((cropSegmentFrameData.Left + cropSegmentFrameOffset.x) - ((static_cast<double>(vi.width) - cropSegmentFrameData.Width) / 2.0));

        totalBorderX = MathHelpers::RoundToNearestEvenIntegral(static_cast<double>(vi.width) - cropSegmentFrameData.Width);
        calculatedRenderData.BorderLeftRight = totalBorderX / 2;
    }
    else
    {
        env->ThrowError(PLUGIN_NAME ": Failed to calculate render data for a single axis-aligned crop.");
    }

    return calculatedRenderData;
}

void VSEProcessorAviSynth::FillYV12Borders(PVideoFrame& videoFrame, const VideoInfo& videoFrameInfo, const int borderLeftRight, const int borderTopBottom, IScriptEnvironment* env)
{
    uint8_t* yPlaneWritePtr = static_cast<uint8_t*>(videoFrame->GetWritePtr(PLANAR_Y));
    uint8_t* uPlaneWritePtr = static_cast<uint8_t*>(videoFrame->GetWritePtr(PLANAR_U));
    uint8_t* vPlaneWritePtr = static_cast<uint8_t*>(videoFrame->GetWritePtr(PLANAR_V));
    const int yPlanePitch = videoFrame->GetPitch(PLANAR_Y);
    const int uPlanePitch = videoFrame->GetPitch(PLANAR_U);
    const int vPlanePitch = videoFrame->GetPitch(PLANAR_V);
    const int blackValueY = 16, blackValueUV = 128;

    int drawResult;
    if (borderLeftRight > 0)
    {
        // Left
        drawResult = libyuv::I420Rect(yPlaneWritePtr, yPlanePitch,
                                      uPlaneWritePtr, uPlanePitch,
                                      vPlaneWritePtr, vPlanePitch,
                                      0, 0,
                                      borderLeftRight, videoFrameInfo.height,
                                      blackValueY, blackValueUV, blackValueUV);
        if (drawResult == 0)
        {
            // Right
            drawResult = libyuv::I420Rect(yPlaneWritePtr, yPlanePitch,
                                          uPlaneWritePtr, uPlanePitch,
                                          vPlaneWritePtr, vPlanePitch,
                                          videoFrameInfo.width - borderLeftRight, 0,
                                          borderLeftRight, videoFrameInfo.height,
                                          blackValueY, blackValueUV, blackValueUV);
        }

        if (drawResult == -1)
        {
            env->ThrowError(PLUGIN_NAME ": Failed fill left and right borders.");
        }
    }

    if (borderTopBottom > 0)
    {
        // Top
        drawResult = libyuv::I420Rect(yPlaneWritePtr, yPlanePitch,
                                      uPlaneWritePtr, uPlanePitch,
                                      vPlaneWritePtr, vPlanePitch,
                                      0, 0,
                                      videoFrameInfo.width, borderTopBottom,
                                      blackValueY, blackValueUV, blackValueUV);
        if (drawResult == 0)
        {
            // Bottom
            drawResult = libyuv::I420Rect(yPlaneWritePtr, yPlanePitch,
                                          uPlaneWritePtr, uPlanePitch,
                                          vPlaneWritePtr, vPlanePitch,
                                          0, videoFrameInfo.height - borderTopBottom,
                                          videoFrameInfo.width, borderTopBottom,
                                          blackValueY, blackValueUV, blackValueUV);
        }

        if (drawResult == -1)
        {
            env->ThrowError(PLUGIN_NAME ": Failed fill top and bottom borders.");
        }
    }
}

PClip VSEProcessorAviSynth::OverlayBorders(const PClip& sourceClip, const int borderLeftRight, const int borderTopBottom, IScriptEnvironment* env)
{
    // Tried to use libyuv::ARGBRect to more efficiently create the mask image,
    // but it resulted in a noticeable partial white line on the bottom edge of the video frame for no apparent reason.
    // So, resorting to the slower pixel by pixel method for the time being...

    VideoInfo overlayFrameInfo = sourceClip->GetVideoInfo();
    overlayFrameInfo.pixel_type = VideoInfo::CS_BGR24;
    overlayFrameInfo.num_frames = 1;

    PVideoFrame borderFrame = env->NewVideoFrame(overlayFrameInfo);
    PVideoFrame maskFrame = env->NewVideoFrame(overlayFrameInfo);
    BYTE* borderFrameWritePtr = borderFrame->GetWritePtr();
    BYTE* maskFrameWritePtr = maskFrame->GetWritePtr();

    const int borderFramePitch = borderFrame->GetPitch();
    const int borderFrameWidth = borderFrame->GetRowSize();
    const int borderFrameHeight = borderFrame->GetHeight();
    const int borderFrameComponents = overlayFrameInfo.NumComponents();
    const int borderFrameBpp = overlayFrameInfo.BytesFromPixels(1);

    for (int h = 0, y = (borderFrameHeight - 1); h < borderFrameHeight; h++, y--)
    {
        for (int w = 0, x = 0; w < borderFrameWidth; w += borderFrameBpp, x++)
        {
            int borderPixelColor;
            int maskPixelColor;

            if (y < borderTopBottom || y >= (overlayFrameInfo.height - borderTopBottom) || x < borderLeftRight || x >= (overlayFrameInfo.width - borderLeftRight))
            {
                borderPixelColor = 0;   // Black
                maskPixelColor = 255;   // White
            }
            else
            {
                borderPixelColor = 255; // White
                maskPixelColor = 0;     // Black
            }

            for (int colorComponent = 0; colorComponent < borderFrameComponents; colorComponent++)
            {
                borderFrameWritePtr[w + colorComponent] = borderPixelColor;
                maskFrameWritePtr[w + colorComponent] = maskPixelColor;
            }
        }

        borderFrameWritePtr += borderFramePitch;
        maskFrameWritePtr += borderFramePitch;
    }

    PClip borderOverlayClip = new SingleFrameClip(overlayFrameInfo, borderFrame);
    PClip borderMaskClip = new SingleFrameClip(overlayFrameInfo, maskFrame);

    return InvokeAvsOverlayFilter(env, sourceClip, borderOverlayClip, 0, 0, borderMaskClip);
}

PClip VSEProcessorAviSynth::InvokeAvsOverlayFilter(IScriptEnvironment* env, const PClip& baseClip, const PClip& overlayClip, const int overlayOffsetX, const int overlayOffsetY, const AVSValue maskClip)
{
    const char* argNames[] = { nullptr, nullptr, "x", "y", "mask", "ignore_conditional", "use444" };
    AVSValue argVals[] = { baseClip, overlayClip, overlayOffsetX, overlayOffsetY, maskClip, true, false };
    return InvokeAvsFilter(env, "Overlay", AVSValue(argVals, ARRAYSIZE(argVals)), argNames);
}

PClip VSEProcessorAviSynth::InvokeAvsColorConversionFilter(IScriptEnvironment* env, const char* colorConversionFilterName, const PClip& sourceClip)
{
    const char* argNames[] = { nullptr, "matrix" };
    AVSValue argVals[] = { sourceClip, (sourceClip->GetVideoInfo().height < 720) ? "Rec601" : "Rec709" };
    return InvokeAvsFilter(env, colorConversionFilterName, AVSValue(argVals, ARRAYSIZE(argVals)), argNames);
}

PClip VSEProcessorAviSynth::InvokeAvsFilter(IScriptEnvironment* env, const char* filterName, const AVSValue filterArgs, const char* filterArgNames[])
{
    AVSValue filterOutput;
    if (!env->InvokeTry(&filterOutput, filterName, filterArgs, filterArgNames))
    {
        env->ThrowError(PLUGIN_NAME ": %s filter not found", filterName);
    }

    return filterOutput.AsClip();
}

AVSValue __cdecl VSEProcessorAviSynth::Create(AVSValue args, void* user_data, IScriptEnvironment* env)
{
    return new VSEProcessorAviSynth(args[0].AsClip(), args[1].AsString(""), env);
}

const AVS_Linkage* AVS_linkage = nullptr;   // for dynamic linkage

extern "C" __declspec(dllexport) const char* __stdcall AvisynthPluginInit3(IScriptEnvironment* env, const AVS_Linkage* const vectors)
{
    AVS_linkage = vectors;
    env->AddFunction(PLUGIN_NAME, "c[projectFileName]s", VSEProcessorAviSynth::Create, nullptr);
    return PLUGIN_NAME " plugin";
}