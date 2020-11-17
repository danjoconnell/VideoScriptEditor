#pragma once

namespace ElementNames
{
    constexpr auto Segment = "Segment";
    constexpr auto StartFrame = "StartFrame";
    constexpr auto EndFrame = "EndFrame";
    constexpr auto TrackNumber = "TrackNumber";
    
    constexpr auto KeyFrames = "KeyFrames";
    constexpr auto KeyFrame = "KeyFrame";
    constexpr auto FrameNumber = "FrameNumber";

    constexpr auto Left = "Left";
    constexpr auto Top = "Top";
    constexpr auto Width = "Width";
    constexpr auto Height = "Height";

    constexpr auto Cropping = "Cropping";
    constexpr auto CropSegments = "CropSegments";
    constexpr auto Angle = "Angle";

    constexpr auto Masking = "Masking";
    constexpr auto MaskingShapes = "Shapes";
    constexpr auto Points = "Points";

    constexpr auto CenterPoint = "CenterPoint";
    constexpr auto RadiusX = "RadiusX";
    constexpr auto RadiusY = "RadiusY";

    constexpr auto VideoProcessingOptions = "VideoProcessingOptions";
    constexpr auto OutputVideoResizeMode = "OutputVideoResizeMode";
    constexpr auto OutputVideoSize = "OutputVideoSize";
    constexpr auto OutputVideoAspectRatio = "OutputVideoAspectRatio";

    constexpr auto RatioNumerator = "Numerator";
    constexpr auto RatioDenominator = "Denominator";

    constexpr auto SystemDrawingSizeWidth = "width";
    constexpr auto SystemDrawingSizeHeight = "height";

    constexpr auto PointD = "PointD";
    constexpr auto PointDx = "x";
    constexpr auto PointDy = "y";
}

namespace AttributeNames
{
    constexpr auto XsiType = "type";
}

namespace SegmentTypeAttributeValues
{
    constexpr auto Crop = "Crop";
    constexpr auto MaskEllipse = "Ellipse";
    constexpr auto MaskPolygon = "Polygon";
    constexpr auto MaskRectangle = "Rectangle";
}

namespace OutputVideoResizeModeElementValues
{
    constexpr auto None = "None";
    constexpr auto LetterboxToSize = "LetterboxToSize";
    constexpr auto LetterboxToAspectRatio = "LetterboxToAspectRatio";
}