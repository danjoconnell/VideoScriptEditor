#include "pch.h"
#include "VSEProjectFileParser.h"
#include "VSEProjectFileElementNames.h"

using namespace VideoScriptEditor::Unmanaged;
using namespace tinyxml2;

VSEProjectFileParser::VSEProjectFileParser(VSEProject& project)
    : _projectRef(project)
{
}

void VSEProjectFileParser::Parse(const char* projectFileName)
{
    std::unique_ptr<tinyxml2::XMLDocument> projectXmlDoc = std::make_unique<tinyxml2::XMLDocument>();

    if (projectXmlDoc->LoadFile(projectFileName) != XML_SUCCESS)
    {
        throw std::runtime_error("Unable to load the project file for parsing");
    }

    const XMLElement* projectXml = projectXmlDoc->RootElement();
    
    const XMLElement* croppingElement = projectXml->FirstChildElement(ElementNames::Cropping);
    if (croppingElement != nullptr)
    {
        ParseCroppingElement(croppingElement);
    }

    const XMLElement* maskingElement = projectXml->FirstChildElement(ElementNames::Masking);
    if (maskingElement != nullptr)
    {
        ParseMaskingElement(maskingElement);
    }

    if (!_projectRef.SegmentModels.empty())
    {
        std::sort(_projectRef.SegmentModels.begin(), _projectRef.SegmentModels.end());
    }

    const XMLElement* videoProcessingOptionsElement = GetChildElementOrThrow(projectXml, ElementNames::VideoProcessingOptions);
    ParseVideoProcessingOptionsElement(videoProcessingOptionsElement);
}

void VSEProjectFileParser::ParseCroppingElement(const XMLElement* croppingElement)
{
    const XMLElement* cropSegmentsElement = croppingElement->FirstChildElement(ElementNames::CropSegments);
    if (cropSegmentsElement == nullptr || cropSegmentsElement->NoChildren())
    {
        return;
    }

    const XMLElement* segmentElement = cropSegmentsElement->FirstChildElement(ElementNames::Segment);
    while (segmentElement != nullptr)
    {
        if (GetXsiTypeAttributeValue(segmentElement) != SegmentTypeAttributeValues::Crop)
        {
            ThrowXmlElementParseException(segmentElement);
        }

        const XMLElement* keyFramesElement = GetChildElementOrThrow(segmentElement, ElementNames::KeyFrames);
        if (keyFramesElement->NoChildren())
        {
            ThrowXmlElementParseException(keyFramesElement);
        }

        auto [startFrame, endFrame, trackNumber] = ParseCommonSegmentElementChildValues(segmentElement);
        if (trackNumber > 0)
        {
            // Multi-segment frame cropping
            _projectRef.NeedsDirect2DProcessing = true;
        }

        SegmentModel segmentModel(SegmentType::Crop, startFrame, endFrame, trackNumber);

        const XMLElement* keyFrameElement = keyFramesElement->FirstChildElement(ElementNames::KeyFrame);
        while (keyFrameElement != nullptr)
        {
            if (GetXsiTypeAttributeValue(keyFrameElement) != SegmentTypeAttributeValues::Crop)
            {
                ThrowXmlElementParseException(keyFrameElement);
            }

            int frameNumber = ParseChildElementTextAsInt(keyFrameElement, ElementNames::FrameNumber);
            double left = ParseChildElementTextAsDouble(keyFrameElement, ElementNames::Left);
            double top = ParseChildElementTextAsDouble(keyFrameElement, ElementNames::Top);
            double width = ParseChildElementTextAsDouble(keyFrameElement, ElementNames::Width);
            double height = ParseChildElementTextAsDouble(keyFrameElement, ElementNames::Height);
            double angle = ParseChildElementTextAsDouble(keyFrameElement, ElementNames::Angle);
            segmentModel.KeyFrames.emplace(frameNumber, std::make_shared<CropKeyFrameModel>(frameNumber, left, top, width, height, angle));

            if (static_cast<float>(angle) != 0.f)
            {
                // Rotation cropping
                _projectRef.NeedsDirect2DProcessing = true;
            }

            keyFrameElement = keyFrameElement->NextSiblingElement(ElementNames::KeyFrame);
        }

        _projectRef.SegmentModels.push_back(std::move(segmentModel));

        segmentElement = segmentElement->NextSiblingElement(ElementNames::Segment);
    }
}

void VSEProjectFileParser::ParseMaskingElement(const XMLElement* maskingElement)
{
    const XMLElement* maskShapesElement = maskingElement->FirstChildElement(ElementNames::MaskingShapes);
    if (maskShapesElement == nullptr || maskShapesElement->NoChildren())
    {
        return;
    }

    const XMLElement* segmentElement = maskShapesElement->FirstChildElement(ElementNames::Segment);
    while (segmentElement != nullptr)
    {
        const XMLElement* keyFramesElement = GetChildElementOrThrow(segmentElement, ElementNames::KeyFrames);
        if (keyFramesElement->NoChildren())
        {
            ThrowXmlElementParseException(keyFramesElement);
        }

        auto [startFrame, endFrame, trackNumber] = ParseCommonSegmentElementChildValues(segmentElement);

        std::string_view segmentTypeString = GetXsiTypeAttributeValue(segmentElement);
        SegmentType segmentType = ParseSegmentTypeString(segmentTypeString);

        SegmentModel segmentModel(segmentType, startFrame, endFrame, trackNumber);
        const XMLElement* keyFrameElement = keyFramesElement->FirstChildElement(ElementNames::KeyFrame);
        while (keyFrameElement != nullptr)
        {
            if (GetXsiTypeAttributeValue(keyFrameElement) != segmentTypeString)
            {
                ThrowXmlElementParseException(keyFrameElement);
            }

            std::shared_ptr<KeyFrameModelBase> keyFrameModel;

            switch (segmentType)
            {
            case SegmentType::MaskEllipse:
                keyFrameModel = ParseMaskingEllipseKeyFrameElement(keyFrameElement);
                break;
            case SegmentType::MaskPolygon:
                keyFrameModel = ParseMaskingPolygonKeyFrameElement(keyFrameElement);
                break;
            case SegmentType::MaskRectangle:
                keyFrameModel = ParseMaskingRectangleKeyFrameElement(keyFrameElement);
                break;
            default:
                ThrowXmlElementParseException(keyFrameElement);
            }

            segmentModel.KeyFrames.emplace(keyFrameModel->FrameNumber, std::move(keyFrameModel));

            keyFrameElement = keyFrameElement->NextSiblingElement(ElementNames::KeyFrame);
        }

        _projectRef.SegmentModels.push_back(std::move(segmentModel));

        _projectRef.NeedsDirect2DProcessing = true;

        segmentElement = segmentElement->NextSiblingElement(ElementNames::Segment);
    }
}

std::shared_ptr<KeyFrameModelBase> VSEProjectFileParser::ParseMaskingEllipseKeyFrameElement(const XMLElement* ellipseKeyFrameElement)
{
    int frameNumber = ParseChildElementTextAsInt(ellipseKeyFrameElement, ElementNames::FrameNumber);

    const XMLElement* centerPointElement = GetChildElementOrThrow(ellipseKeyFrameElement, ElementNames::CenterPoint);
    if (centerPointElement->NoChildren())
    {
        ThrowXmlElementParseException(centerPointElement);
    }

    bool hasCenterPointX = false, hasCenterPointY = false;
    double centerPointX = 0.0, centerPointY = 0.0;

    const XMLElement* pointChildElement = centerPointElement->FirstChildElement();
    while (pointChildElement != nullptr)
    {
        std::string_view pointChildElementName(pointChildElement->Name());
        if (pointChildElementName.ends_with(ElementNames::PointDx))
        {
            hasCenterPointX = pointChildElement->QueryDoubleText(&centerPointX) == XML_SUCCESS;
            if (!hasCenterPointX)
            {
                ThrowXmlElementParseException(pointChildElement);
            }
        }
        else if (pointChildElementName.ends_with(ElementNames::PointDy))
        {
            hasCenterPointY = pointChildElement->QueryDoubleText(&centerPointY) == XML_SUCCESS;
            if (!hasCenterPointY)
            {
                ThrowXmlElementParseException(pointChildElement);
            }
        }

        pointChildElement = pointChildElement->NextSiblingElement();
    }

    if (!hasCenterPointX || !hasCenterPointY)
    {
        ThrowXmlElementParseException(centerPointElement);
    }

    double radiusX = ParseChildElementTextAsDouble(ellipseKeyFrameElement, ElementNames::RadiusX);
    double radiusY = ParseChildElementTextAsDouble(ellipseKeyFrameElement, ElementNames::RadiusY);
    return std::make_shared<MaskEllipseKeyFrameModel>(frameNumber, PointD(centerPointX, centerPointY), radiusX, radiusY);
}

std::shared_ptr<KeyFrameModelBase> VSEProjectFileParser::ParseMaskingPolygonKeyFrameElement(const tinyxml2::XMLElement* polygonKeyFrameElement)
{
    int frameNumber = ParseChildElementTextAsInt(polygonKeyFrameElement, ElementNames::FrameNumber);

    const XMLElement* pointsElement = GetChildElementOrThrow(polygonKeyFrameElement, ElementNames::Points);
    if (pointsElement->NoChildren())
    {
        ThrowXmlElementParseException(pointsElement);
    }

    auto maskPolygonKeyFrameModel = std::make_shared<MaskPolygonKeyFrameModel>(frameNumber);

    const XMLElement* pointElement = pointsElement->FirstChildElement();
    while (pointElement != nullptr)
    {
        if (std::string_view(pointElement->Name()).ends_with(ElementNames::PointD) && !pointElement->NoChildren())
        {
            bool hasX = false, hasY = false;
            double pointX, pointY;

            const XMLElement* pointChildElement = pointElement->FirstChildElement();
            while (pointChildElement != nullptr)
            {
                std::string_view pointChildElementName(pointChildElement->Name());
                if (pointChildElementName.ends_with(ElementNames::PointDx))
                {
                    hasX = pointChildElement->QueryDoubleText(&pointX) == XML_SUCCESS;
                    if (!hasX)
                    {
                        ThrowXmlElementParseException(pointChildElement);
                    }
                }
                else if (pointChildElementName.ends_with(ElementNames::PointDy))
                {
                    hasY = pointChildElement->QueryDoubleText(&pointY) == XML_SUCCESS;
                    if (!hasY)
                    {
                        ThrowXmlElementParseException(pointChildElement);
                    }
                }

                pointChildElement = pointChildElement->NextSiblingElement();
            }

            if (hasX && hasY)
            {
                maskPolygonKeyFrameModel->Points.emplace_back(pointX, pointY);
            }
            else
            {
                ThrowXmlElementParseException(pointElement);
            }
        }

        pointElement = pointElement->NextSiblingElement();
    }

    if (maskPolygonKeyFrameModel->Points.empty())
    {
        ThrowXmlElementParseException(pointsElement);
    }

    return maskPolygonKeyFrameModel;
}

std::shared_ptr<KeyFrameModelBase> VSEProjectFileParser::ParseMaskingRectangleKeyFrameElement(const XMLElement* rectangleKeyFrameElement)
{
    int frameNumber = ParseChildElementTextAsInt(rectangleKeyFrameElement, ElementNames::FrameNumber);
    double left = ParseChildElementTextAsDouble(rectangleKeyFrameElement, ElementNames::Left);
    double top = ParseChildElementTextAsDouble(rectangleKeyFrameElement, ElementNames::Top);
    double width = ParseChildElementTextAsDouble(rectangleKeyFrameElement, ElementNames::Width);
    double height = ParseChildElementTextAsDouble(rectangleKeyFrameElement, ElementNames::Height);
    return std::make_shared<MaskRectangleKeyFrameModel>(frameNumber, left, top, width, height);
}

void VSEProjectFileParser::ParseVideoProcessingOptionsElement(const XMLElement* videoProcessingOptionsElement)
{
    VideoProcessingOptionsModel& videoProcessingOptions = _projectRef.VideoProcessingOptions;

    const XMLElement* videoResizeModeElement = videoProcessingOptionsElement->FirstChildElement(ElementNames::OutputVideoResizeMode);
    if (videoResizeModeElement != nullptr)
    {
        std::string_view videoResizeModeElementValue(videoResizeModeElement->GetText());

        if (videoResizeModeElementValue == OutputVideoResizeModeElementValues::LetterboxToSize)
        {
            videoProcessingOptions.OutputVideoResizeMode = VideoResizeMode::LetterboxToSize;
        }
        else if (videoResizeModeElementValue == OutputVideoResizeModeElementValues::LetterboxToAspectRatio)
        {
            videoProcessingOptions.OutputVideoResizeMode = VideoResizeMode::LetterboxToAspectRatio;
        }
    }

    if (videoProcessingOptions.OutputVideoResizeMode == VideoResizeMode::LetterboxToSize)
    {
        const XMLElement* videoSizeElement = videoProcessingOptionsElement->FirstChildElement(ElementNames::OutputVideoSize);
        if (videoSizeElement->NoChildren())
        {
            ThrowXmlElementParseException(videoSizeElement);
        }

        const XMLElement* childElement = videoSizeElement->FirstChildElement();
        while (childElement != nullptr)
        {
            std::string_view childElementName(childElement->Name());
            if (childElementName.ends_with(ElementNames::SystemDrawingSizeWidth))
            {
                if (childElement->QueryUnsignedText(&videoProcessingOptions.OutputVideoSize.width) != XML_SUCCESS)
                {
                    ThrowXmlElementParseException(childElement);
                }
            }
            else if (childElementName.ends_with(ElementNames::SystemDrawingSizeHeight))
            {
                if (childElement->QueryUnsignedText(&videoProcessingOptions.OutputVideoSize.height) != XML_SUCCESS)
                {
                    ThrowXmlElementParseException(childElement);
                }
            }

            childElement = childElement->NextSiblingElement();
        }

        if (videoProcessingOptions.OutputVideoSize.width == 0 || videoProcessingOptions.OutputVideoSize.height == 0)
        {
            ThrowXmlElementParseException(videoSizeElement);
        }
    }
    else if (videoProcessingOptions.OutputVideoResizeMode == VideoResizeMode::LetterboxToAspectRatio)
    {
        const XMLElement* aspectRatioElement = videoProcessingOptionsElement->FirstChildElement(ElementNames::OutputVideoAspectRatio);
        if (aspectRatioElement->NoChildren())
        {
            ThrowXmlElementParseException(aspectRatioElement);
        }

        const XMLElement* childElement = aspectRatioElement->FirstChildElement();
        while (childElement != nullptr)
        {
            std::string_view childElementName(childElement->Name());
            if (childElementName.ends_with(ElementNames::RatioNumerator))
            {
                if (childElement->QueryUnsignedText(&videoProcessingOptions.OutputAspectRatio.Numerator) != XML_SUCCESS)
                {
                    ThrowXmlElementParseException(childElement);
                }
            }
            else if (childElementName.ends_with(ElementNames::RatioDenominator))
            {
                if (childElement->QueryUnsignedText(&videoProcessingOptions.OutputAspectRatio.Denominator) != XML_SUCCESS)
                {
                    ThrowXmlElementParseException(childElement);
                }
            }

            childElement = childElement->NextSiblingElement();
        }

        if (videoProcessingOptions.OutputAspectRatio.Numerator == 0 || videoProcessingOptions.OutputAspectRatio.Denominator == 0)
        {
            ThrowXmlElementParseException(aspectRatioElement);
        }
    }
}

std::tuple<int, int, int> VSEProjectFileParser::ParseCommonSegmentElementChildValues(const XMLElement* segmentElement)
{
    return std::make_tuple(
        ParseChildElementTextAsInt(segmentElement, ElementNames::StartFrame),
        ParseChildElementTextAsInt(segmentElement, ElementNames::EndFrame),
        ParseChildElementTextAsInt(segmentElement, ElementNames::TrackNumber)
    );
}

constexpr SegmentType VSEProjectFileParser::ParseSegmentTypeString(const std::string_view& segmentTypeString)
{
    if (segmentTypeString == SegmentTypeAttributeValues::Crop)
    {
        return SegmentType::Crop;
    }
    else if (segmentTypeString == SegmentTypeAttributeValues::MaskEllipse)
    {
        return SegmentType::MaskEllipse;
    }
    else if (segmentTypeString == SegmentTypeAttributeValues::MaskPolygon)
    {
        return SegmentType::MaskPolygon;
    }
    else if (segmentTypeString == SegmentTypeAttributeValues::MaskRectangle)
    {
        return SegmentType::MaskRectangle;
    }
    else
    {
        throw std::runtime_error(fmt::format("Unrecognized Type value '{:s}'", segmentTypeString.data()));
    }
}

inline int VSEProjectFileParser::ParseChildElementTextAsInt(const tinyxml2::XMLElement* parentElement, const char* childElementName)
{
    const XMLElement* childElement = GetChildElementOrThrow(parentElement, childElementName);

    int childElementValue;
    if (childElement->QueryIntText(&childElementValue) != XML_SUCCESS)
    {
        ThrowXmlElementParseException(childElement);
    }

    return childElementValue;
}

inline double VSEProjectFileParser::ParseChildElementTextAsDouble(const tinyxml2::XMLElement* parentElement, const char* childElementName)
{
    const XMLElement* childElement = GetChildElementOrThrow(parentElement, childElementName);

    double childElementValue;
    if (childElement->QueryDoubleText(&childElementValue) != XML_SUCCESS)
    {
        ThrowXmlElementParseException(childElement);
    }

    return childElementValue;
}

inline std::string_view VSEProjectFileParser::GetXsiTypeAttributeValue(const tinyxml2::XMLElement* xmlElement)
{
    std::string_view attributeValue;

    const XMLAttribute* xmlAttribute = xmlElement->FirstAttribute();
    while (xmlAttribute != nullptr)
    {
        std::string_view attributeName(xmlAttribute->Name());
        if (attributeName.ends_with(AttributeNames::XsiType))
        {
            attributeValue = xmlAttribute->Value();
            break;
        }

        xmlAttribute = xmlAttribute->Next();
    }

    return attributeValue;
}

inline const tinyxml2::XMLElement* VSEProjectFileParser::GetChildElementOrThrow(const tinyxml2::XMLElement* parentElement, const char* childElementName)
{
    const XMLElement* childElement = parentElement->FirstChildElement(childElementName);
    if (childElement == nullptr)
    {
        ThrowXmlElementParseException(parentElement);
    }

    return childElement;
}

inline void VSEProjectFileParser::ThrowXmlElementParseException(const tinyxml2::XMLElement* xmlElement)
{
    throw std::runtime_error(fmt::format("Error parsing XML element '{:s}' at line {:d}", xmlElement->Name(), xmlElement->GetLineNum()));
}