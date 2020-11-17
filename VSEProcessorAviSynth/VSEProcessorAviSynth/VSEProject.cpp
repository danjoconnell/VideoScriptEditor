#include "pch.h"
#include "VSEProject.h"

using namespace VideoScriptEditor::Unmanaged;
using namespace std;

void CropKeyFrameModel::SetFrameDataItemFromLerpedKeyFrames(const std::shared_ptr<CropKeyFrameModel>& keyFrameToLerpTo, const double lerpAmount, VideoScriptEditor::Unmanaged::CropSegmentFrameDataItem& frameDataItem)
{
    double cropLeft, cropTop, cropWidth, cropHeight, cropAngle;

    if (keyFrameToLerpTo != nullptr && lerpAmount > 0.0)
    {
        cropLeft = std::lerp(Left, keyFrameToLerpTo->Left, lerpAmount);
        cropTop = std::lerp(Top, keyFrameToLerpTo->Top, lerpAmount);
        cropWidth = std::lerp(Width, keyFrameToLerpTo->Width, lerpAmount);
        cropHeight = std::lerp(Height, keyFrameToLerpTo->Height, lerpAmount);
        cropAngle = std::lerp(Angle, keyFrameToLerpTo->Angle, lerpAmount);
    }
    else
    {
        cropLeft = Left;
        cropTop = Top;
        cropWidth = Width;
        cropHeight = Height;
        cropAngle = Angle;
    }

    if (frameDataItem.Left != cropLeft)
    {
        frameDataItem.Left = cropLeft;
    }

    if (frameDataItem.Top != cropTop)
    {
        frameDataItem.Top = cropTop;
    }

    if (frameDataItem.Width != cropWidth)
    {
        frameDataItem.Width = cropWidth;
    }

    if (frameDataItem.Height != cropHeight)
    {
        frameDataItem.Height = cropHeight;
    }

    if (frameDataItem.Angle != cropAngle)
    {
        frameDataItem.Angle = cropAngle;
    }
}

bool MaskEllipseKeyFrameModel::SetFrameDataItemFromLerpedKeyFrames(const std::shared_ptr<MaskKeyFrameModelBase>& keyFrameToLerpTo, const double lerpAmount, std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>& frameDataItem)
{
    bool frameDataItemWasSet = false;

    PointD ellipseCenterPoint = {0};
    double ellipseRadiusX, ellipseRadiusY;

    auto ellipseKeyFrameToLerpTo = dynamic_pointer_cast<MaskEllipseKeyFrameModel>(keyFrameToLerpTo);
    if (ellipseKeyFrameToLerpTo != nullptr && lerpAmount > 0.0)
    {
        ellipseCenterPoint = MathHelpers::Lerp(CenterPoint, ellipseKeyFrameToLerpTo->CenterPoint, lerpAmount);
        ellipseRadiusX = std::lerp(RadiusX, ellipseKeyFrameToLerpTo->RadiusX, lerpAmount);
        ellipseRadiusY = std::lerp(RadiusY, ellipseKeyFrameToLerpTo->RadiusY, lerpAmount);
    }
    else
    {
        ellipseCenterPoint = CenterPoint;
        ellipseRadiusX = RadiusX;
        ellipseRadiusY = RadiusY;
    }

    std::shared_ptr<MaskEllipseSegmentFrameDataItem> ellipseFrameDataItem = dynamic_pointer_cast<MaskEllipseSegmentFrameDataItem>(frameDataItem);
    if (!ellipseFrameDataItem)
    {
        frameDataItem.reset(new MaskEllipseSegmentFrameDataItem(ellipseCenterPoint, ellipseRadiusX, ellipseRadiusY));
        frameDataItemWasSet = true;
    }
    else if (ellipseFrameDataItem->CenterPoint != ellipseCenterPoint || ellipseFrameDataItem->RadiusX != ellipseRadiusX || ellipseFrameDataItem->RadiusY != ellipseRadiusY)
    {
        ellipseFrameDataItem->CenterPoint.X = ellipseCenterPoint.X;
        ellipseFrameDataItem->CenterPoint.Y = ellipseCenterPoint.Y;
        ellipseFrameDataItem->RadiusX = ellipseRadiusX;
        ellipseFrameDataItem->RadiusY = ellipseRadiusY;

        frameDataItemWasSet = true;
    }

    return frameDataItemWasSet;
}

bool MaskPolygonKeyFrameModel::SetFrameDataItemFromLerpedKeyFrames(const std::shared_ptr<MaskKeyFrameModelBase>& keyFrameToLerpTo, const double lerpAmount, std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>& frameDataItem)
{
    bool frameDataItemWasSet = false;

    size_t polygonPointsCount = Points.size();
    auto polygonKeyFrameToLerpTo = dynamic_pointer_cast<MaskPolygonKeyFrameModel>(keyFrameToLerpTo);
#ifdef _DEBUG
    if (polygonKeyFrameToLerpTo != nullptr)
        assert(polygonPointsCount == polygonKeyFrameToLerpTo->Points.size() && polygonPointsCount > 0);
    else
        assert(polygonPointsCount > 0);
#endif

    std::vector<PointD> polygonPoints;
    if (polygonKeyFrameToLerpTo != nullptr && lerpAmount > 0.0)
    {
        polygonPoints.reserve(polygonPointsCount);
        for (size_t i = 0; i < polygonPointsCount; ++i)
        {
            polygonPoints.push_back(
                MathHelpers::Lerp(Points[i], polygonKeyFrameToLerpTo->Points[i], lerpAmount)
            );
        }
    }
    else
    {
        polygonPoints = Points;
    }

    std::shared_ptr<MaskPolygonSegmentFrameDataItem> polygonFrameDataItem = dynamic_pointer_cast<MaskPolygonSegmentFrameDataItem>(frameDataItem);
    if (!polygonFrameDataItem)
    {
        frameDataItem.reset(new MaskPolygonSegmentFrameDataItem(std::move(polygonPoints)));
        frameDataItemWasSet = true;
    }
    else if (polygonFrameDataItem->Points != polygonPoints)
    {
        polygonFrameDataItem->Points = std::move(polygonPoints);
        frameDataItemWasSet = true;
    }

    return frameDataItemWasSet;
}

bool MaskRectangleKeyFrameModel::SetFrameDataItemFromLerpedKeyFrames(const std::shared_ptr<MaskKeyFrameModelBase>& keyFrameToLerpTo, const double lerpAmount, std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>& frameDataItem)
{
    bool frameDataItemWasSet = false;

    double rectLeft, rectTop, rectWidth, rectHeight;

    auto rectangleKeyFrameToLerpTo = dynamic_pointer_cast<MaskRectangleKeyFrameModel>(keyFrameToLerpTo);
    if (rectangleKeyFrameToLerpTo != nullptr && lerpAmount > 0.0)
    {
        rectLeft = std::lerp(Left, rectangleKeyFrameToLerpTo->Left, lerpAmount);
        rectTop = std::lerp(Top, rectangleKeyFrameToLerpTo->Top, lerpAmount);
        rectWidth = std::lerp(Width, rectangleKeyFrameToLerpTo->Width, lerpAmount);
        rectHeight = std::lerp(Height, rectangleKeyFrameToLerpTo->Height, lerpAmount);
    }
    else
    {
        rectLeft = Left;
        rectTop = Top;
        rectWidth = Width;
        rectHeight = Height;
    }

    std::shared_ptr<MaskRectangleSegmentFrameDataItem> rectangleFrameDataItem = dynamic_pointer_cast<MaskRectangleSegmentFrameDataItem>(frameDataItem);
    if (!rectangleFrameDataItem)
    {
        frameDataItem.reset(new MaskRectangleSegmentFrameDataItem(rectLeft,
                                                                  rectTop,
                                                                  rectWidth,
                                                                  rectHeight));
        frameDataItemWasSet = true;
    }
    else if (rectangleFrameDataItem->Left != rectLeft || rectangleFrameDataItem->Top != rectTop
            || rectangleFrameDataItem->Width != rectWidth || rectangleFrameDataItem->Height != rectHeight)
    {
        rectangleFrameDataItem->Left = rectLeft;
        rectangleFrameDataItem->Top = rectTop;
        rectangleFrameDataItem->Width = rectWidth;
        rectangleFrameDataItem->Height = rectHeight;

        frameDataItemWasSet = true;
    }

    return frameDataItemWasSet;
}