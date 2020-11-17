#pragma once

/// <summary>
/// Parses the XML content of a Video Script Editor project file
/// into a <see cref="VSEProject"/> data structure.
/// </summary>
class VSEProjectFileParser
{
    /// <summary>Reference to the <see cref="VSEProject"/> structure that will receive the parsed data values.</summary>
    VSEProject& _projectRef;

public:
    /// <summary>
    /// Creates a new <see cref="VSEProjectFileParser"/> instance
    /// containing a reference to the <see cref="VSEProject"/> structure that will receive the parsed data values.
    /// </summary>
    /// <param name="project">
    /// A reference to the <see cref="VSEProject"/> structure that will receive the parsed data values.
    /// </param>
    VSEProjectFileParser(VSEProject& project);

    /// <summary>
    /// Opens the specified Video Script Editor project file 
    /// and parses the XML content into the <see cref="VSEProject"/> structure reference.
    /// </summary>
    /// <param name="projectFileName">The file path of the Video Script Editor project file to parse.</param>
    void Parse(const char* projectFileName);

private:
    /// <summary>
    /// Parses the Cropping <see cref="tinyxml2::XMLElement"/>, creating and adding <see cref="SegmentModel"/>s
    /// for each parsed Crop type child Segment element to the <see cref="VSEProject::SegmentModels"/> collection.
    /// </summary>
    /// <param name="croppingElement">A pointer to the Cropping <see cref="tinyxml2::XMLElement"/> to parse.</param>
    void ParseCroppingElement(const tinyxml2::XMLElement* croppingElement);

    /// <summary>
    /// Parses the Masking <see cref="tinyxml2::XMLElement"/>, creating and adding <see cref="SegmentModel"/>s
    /// for each parsed Mask shape type child Segment element to the <see cref="VSEProject::SegmentModels"/> collection.
    /// </summary>
    /// <param name="maskingElement">A pointer to the Masking <see cref="tinyxml2::XMLElement"/> to parse.</param>
    void ParseMaskingElement(const tinyxml2::XMLElement* maskingElement);

    /// <summary>
    /// Parses a masking Ellipse type KeyFrame <see cref="tinyxml2::XMLElement"/>
    /// into a <see cref="MaskEllipseKeyFrameModel"/>.
    /// </summary>
    /// <param name="ellipseKeyFrameElement">A pointer to the KeyFrame <see cref="tinyxml2::XMLElement"/> to parse.</param>
    /// <returns>A smart pointer to the parsed <see cref="MaskEllipseKeyFrameModel"/>.</returns>
    std::shared_ptr<KeyFrameModelBase> ParseMaskingEllipseKeyFrameElement(const tinyxml2::XMLElement* ellipseKeyFrameElement);

    /// <summary>
    /// Parses a masking Polygon type KeyFrame <see cref="tinyxml2::XMLElement"/>
    /// into a <see cref="MaskPolygonKeyFrameModel"/>.
    /// </summary>
    /// <param name="polygonKeyFrameElement">A pointer to the KeyFrame <see cref="tinyxml2::XMLElement"/> to parse.</param>
    /// <returns>A smart pointer to the parsed <see cref="MaskPolygonKeyFrameModel"/>.</returns>
    std::shared_ptr<KeyFrameModelBase> ParseMaskingPolygonKeyFrameElement(const tinyxml2::XMLElement* polygonKeyFrameElement);

    /// <summary>
    /// Parses a masking Rectangle type KeyFrame <see cref="tinyxml2::XMLElement"/>
    /// into a <see cref="MaskRectangleKeyFrameModel"/>.
    /// </summary>
    /// <param name="rectangleKeyFrameElement">A pointer to the KeyFrame <see cref="tinyxml2::XMLElement"/> to parse.</param>
    /// <returns>A smart pointer to the parsed <see cref="MaskRectangleKeyFrameModel"/>.</returns>
    std::shared_ptr<KeyFrameModelBase> ParseMaskingRectangleKeyFrameElement(const tinyxml2::XMLElement* rectangleKeyFrameElement);

    /// <summary>
    /// Parses the VideoProcessingOptions <see cref="tinyxml2::XMLElement"/>,
    /// setting the field values of the <see cref="VSEProject::VideoProcessingOptions"/> model.
    /// </summary>
    /// <param name="videoProcessingOptionsElement">>A pointer to the VideoProcessingOptions <see cref="tinyxml2::XMLElement"/> to parse.</param>
    void ParseVideoProcessingOptionsElement(const tinyxml2::XMLElement* videoProcessingOptionsElement);

    /// <summary>
    /// Parses the child <see cref="tinyxml2::XMLElement"/>s common to all Segment <see cref="tinyxml2::XMLElement"/>s;
    /// StartFrame, EndFrame, TrackNumber.
    /// </summary>
    /// <param name="segmentElement">A pointer to the Segment <see cref="tinyxml2::XMLElement"/> to parse.</param>
    /// <returns>
    /// A <see cref="std::tuple"/> containing the parsed integral StartFrame, EndFrame, TrackNumber values (in that order).
    /// </returns>
    std::tuple<int, int, int> ParseCommonSegmentElementChildValues(const tinyxml2::XMLElement* segmentElement);

    /// <summary>
    /// Converts the specified string representation of a segment type to its <see cref="SegmentType"/> equivalent.
    /// </summary>
    /// <param name="segmentTypeString">A reference to the <see cref="std::string_view"/> representing a <see cref="SegmentType"/>.</param>
    /// <returns>A <see cref="SegmentType"/> equivalent to the segment type contained in <paramref name="segmentTypeString"/>.</returns>
    constexpr SegmentType ParseSegmentTypeString(const std::string_view& segmentTypeString);

    /// <summary>
    /// Parses the value of the named child <see cref="tinyxml2::XMLElement"/> within the specified
    /// parent <see cref="tinyxml2::XMLElement"/> as an integer.
    /// </summary>
    /// <param name="parentElement">A pointer to the parent <see cref="tinyxml2::XMLElement"/>.</param>
    /// <param name="childElementName">The name of the child <see cref="tinyxml2::XMLElement"/> to parse.</param>
    /// <returns>The integer value of the parsed child <see cref="tinyxml2::XMLElement"/>.</returns>
    int ParseChildElementTextAsInt(const tinyxml2::XMLElement* parentElement, const char* childElementName);

    /// <summary>
    /// Parses the value of the named child <see cref="tinyxml2::XMLElement"/> within the specified
    /// parent <see cref="tinyxml2::XMLElement"/> as a double-precision floating point.
    /// </summary>
    /// <param name="parentElement">A pointer to the parent <see cref="tinyxml2::XMLElement"/>.</param>
    /// <param name="childElementName">The name of the child <see cref="tinyxml2::XMLElement"/> to parse.</param>
    /// <returns>The double-precision floating point value of the parsed child <see cref="tinyxml2::XMLElement"/>.</returns>
    double ParseChildElementTextAsDouble(const tinyxml2::XMLElement* parentElement, const char* childElementName);

    /// <summary>
    /// Gets the value of the xsi:type attribute for the specified <see cref="tinyxml2::XMLElement"/>.
    /// </summary>
    /// <param name="xmlElement">A pointer to an <see cref="tinyxml2::XMLElement"/> containing a xsi:type attribute.</param>
    /// <returns>A <see cref="std::string_view"/> containing the value of the xsi:type attribute.</returns>
    std::string_view GetXsiTypeAttributeValue(const tinyxml2::XMLElement* xmlElement);

    /// <summary>
    /// Gets the named child <see cref="tinyxml2::XMLElement"/> of the specified parent <see cref="tinyxml2::XMLElement"/>,
    /// throwing a <see cref="std::runtime_error"/> exception upon failure.
    /// </summary>
    /// <param name="parentElement">A pointer to the parent <see cref="tinyxml2::XMLElement"/>.</param>
    /// <param name="childElementName">The name of the child <see cref="tinyxml2::XMLElement"/>.</param>
    /// <returns>A pointer to the child <see cref="tinyxml2::XMLElement"/>.</returns>
    const tinyxml2::XMLElement* GetChildElementOrThrow(const tinyxml2::XMLElement* parentElement, const char* childElementName);

    /// <summary>
    /// Throws a <see cref="std::runtime_error"/> exception for an error occurring while parsing the specified <see cref="tinyxml2::XMLElement"/>.
    /// </summary>
    /// <param name="xmlElement">A pointer to the <see cref="tinyxml2::XMLElement"/> that failed to be parsed.</param>
    void ThrowXmlElementParseException(const tinyxml2::XMLElement* xmlElement);
};