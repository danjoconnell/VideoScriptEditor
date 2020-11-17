#pragma once

/// <summary>
/// Specifies the method for performing video resize.
/// </summary>
enum class VideoResizeMode
{
    /// <summary>No resize, the original video width and height are retained.</summary>
    None,

    /// <summary>Letterbox to size.</summary>
    LetterboxToSize,

    /// <summary>Letterbox to aspect ratio.</summary>
    LetterboxToAspectRatio
};

/// <summary>
/// Model encapsulating video processing options such as video resizing.
/// </summary>
struct VideoProcessingOptionsModel
{
    /// <summary>Specifies the video resize method.</summary>
    VideoResizeMode OutputVideoResizeMode;

    union
    {
        /// <summary>The desired aspect ratio if the video is to be resized using an aspect ratio.</summary>
        VideoScriptEditor::Unmanaged::Ratio OutputAspectRatio;

        /// <summary>The desired size of the video in pixels.</summary>
        D2D1_SIZE_U OutputVideoSize;
    };

    /// <summary>
    /// Creates a new <see cref="VideoProcessingOptionsModel"/> instance.
    /// </summary>
    VideoProcessingOptionsModel()
    {
        OutputVideoResizeMode = VideoResizeMode::None;
        OutputAspectRatio = {0};
    }
};

/// <summary>
/// Base structure for key frame models.
/// </summary>
struct KeyFrameModelBase
{
    /// <summary>The zero-based frame number of this key frame.</summary>
    int FrameNumber;
    
    /// <summary>
    ///  Base constructor for key frame models derived from the <see cref="KeyFrameModelBase"/> structure.
    /// </summary>
    /// <param name="frameNumber">The zero-based frame number of the key frame.</param>
    KeyFrameModelBase(int frameNumber)
        : FrameNumber(frameNumber)
    {
    }

    /// <summary>
    /// Base destructor for key frame models derived from the <see cref="KeyFrameModelBase"/> structure.
    /// </summary>
    virtual ~KeyFrameModelBase() = default;
};

/// <summary>
/// Returns a value that indicates whether the left hand side <see cref="KeyFrameModelBase"/> instance
/// is less than the right hand side <see cref="KeyFrameModelBase"/> instance.
/// </summary>
/// <remarks>Compares <see cref="KeyFrameModelBase::FrameNumber"/> field values.</remarks>
/// <param name="lhs">The left hand side <see cref="KeyFrameModelBase"/> instance to compare.</param>
/// <param name="rhs">The right hand side <see cref="KeyFrameModelBase"/> instance to compare.</param>
/// <returns>True if <paramref name="lhs"/> is less than <paramref name="rhs"/>; otherwise, False.</returns>
inline bool operator<(const KeyFrameModelBase& lhs, const KeyFrameModelBase& rhs)
{
    return lhs.FrameNumber < rhs.FrameNumber;
}

/// <summary>
/// Describes the type of a segment model.
/// </summary>
enum class SegmentType
{
    Crop = 1,
    MaskEllipse,
    MaskPolygon,
    MaskRectangle
};

/// <summary>
/// Model encapsulating segment data.
/// </summary>
struct SegmentModel
{
    /// <summary>This segment model's type.</summary>
    SegmentType Type;

    /// <summary>The inclusive zero-based start frame number of this segment.</summary>
    int StartFrame;

    /// <summary>The inclusive zero-based end frame number of this segment.</summary>
    int EndFrame;

    /// <summary>The zero-based timeline track number of this segment.</summary>
    int TrackNumber;

    /// <summary>
    /// A <see cref="std::map"/> of key frames in this segment sorted and keyed by zero-based frame number.
    /// </summary>
    /// <remarks>Uses lower_bound for before/after frame lookup.</remarks>
    std::map<int, std::shared_ptr<KeyFrameModelBase>> KeyFrames;

    /// <summary>
    /// Creates a new <see cref="SegmentModel"/> instance.
    /// </summary>
    /// <param name="type">The <see cref="SegmentType"/> describing the type of segment.</param>
    /// <param name="startFrame">The inclusive zero-based start frame number of the segment.</param>
    /// <param name="endFrame">The inclusive zero-based end frame number of the segment.</param>
    /// <param name="trackNumber">The zero-based timeline track number of the segment.</param>
    SegmentModel(SegmentType type, int startFrame, int endFrame, int trackNumber)
        : Type(type), StartFrame(startFrame), EndFrame(endFrame), TrackNumber(trackNumber)
    {
    }
};

/// <summary>
/// Returns a value that indicates whether the left hand side <see cref="SegmentModel"/> instance
/// is less than the right hand side <see cref="SegmentModel"/> instance.
/// </summary>
/// <remarks>Compares <see cref="SegmentModel::StartFrame"/> field values.</remarks>
/// <param name="lhs">The left hand side <see cref="SegmentModel"/> instance to compare.</param>
/// <param name="rhs">The right hand side <see cref="SegmentModel"/> instance to compare.</param>
/// <returns>True if <paramref name="lhs"/> is less than <paramref name="rhs"/>; otherwise, False.</returns>
inline bool operator<(const SegmentModel& lhs, const SegmentModel& rhs)
{
    return lhs.StartFrame < rhs.StartFrame;
}

/// <summary>
/// Model encapsulating cropping segment key frame data.
/// </summary>
struct CropKeyFrameModel : KeyFrameModelBase
{
    /// <summary>The left pixel coordinate of the area to crop.</summary>
    double Left;

    /// <summary>The top pixel coordinate of the area to crop.</summary>
    double Top;

    /// <summary>The pixel width of the area to crop.</summary>
    double Width;

    /// <summary>The pixel height of the area to crop.</summary>
    double Height;

    /// <summary>The angle in degrees at which the crop area is rotated.</summary>
    double Angle;

    /// <summary>
    /// Creates a new <see cref="CropKeyFrameModel"/> instance.
    /// </summary>
    /// <param name="frameNumber">The zero-based frame number of the key frame.</param>
    /// <param name="left">The left pixel coordinate of the area to crop.</param>
    /// <param name="top">The top pixel coordinate of the area to crop.</param>
    /// <param name="width">The pixel width of the area to crop.</param>
    /// <param name="height">The pixel height of the area to crop.</param>
    /// <param name="angle">The angle in degrees at which the crop area is rotated.</param>
    CropKeyFrameModel(int frameNumber, double left, double top, double width, double height, double angle)
        : KeyFrameModelBase(frameNumber), Left(left), Top(top), Width(width), Height(height), Angle(angle)
    {
    }

    /// <summary>
    /// Linearly interpolates between this <see cref="CropKeyFrameModel"/> and another <see cref="CropKeyFrameModel"/>
    /// based on the given weighting and sets the field values of a <see cref="VideoScriptEditor::Unmanaged::CropSegmentFrameDataItem"/>
    /// structure only if its field values differ from the interpolated values.
    /// </summary>
    /// <param name="keyFrameToLerpTo">
    /// (In) A reference to a smart pointer to a <see cref="CropKeyFrameModel"/> with a <see cref="KeyFrameModelBase::FrameNumber"/>
    /// greater than this <see cref="CropKeyFrameModel"/>'s <see cref="KeyFrameModelBase::FrameNumber"/>.
    /// </param>
    /// <param name="lerpAmount">(In) Value indicating the weight of <paramref name="keyFrameToLerpTo"/>.</param>
    /// <param name="frameDataItem">
    /// (In/Out) A reference to the <see cref="VideoScriptEditor::Unmanaged::CropSegmentFrameDataItem"/> structure
    /// to be interpolated to.
    /// </param>
    void SetFrameDataItemFromLerpedKeyFrames(const std::shared_ptr<CropKeyFrameModel>& keyFrameToLerpTo, const double lerpAmount, VideoScriptEditor::Unmanaged::CropSegmentFrameDataItem& frameDataItem);
};

/// <summary>
/// Base structure for masking segment key frame models.
/// </summary>
struct MaskKeyFrameModelBase : KeyFrameModelBase
{
    /// <summary>
    /// Base constructor for masking segment key frame models derived from the <see cref="MaskKeyFrameModelBase"/> structure.
    /// </summary>
    /// <param name="frameNumber">The zero-based frame number of the key frame.</param>
    MaskKeyFrameModelBase(int frameNumber) : KeyFrameModelBase(frameNumber)
    {
    }

    /// <summary>
    /// Linearly interpolates between this <see cref="MaskKeyFrameModelBase"/> and another <see cref="MaskKeyFrameModelBase"/>
    /// based on the given weighting and sets the field values of a <see cref="VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase"/>
    /// structure only if its field values differ from the interpolated values.
    /// </summary>
    /// <param name="keyFrameToLerpTo">
    /// (In) A reference to a smart pointer to a <see cref="MaskKeyFrameModelBase"/> with a <see cref="KeyFrameModelBase::FrameNumber"/>
    /// greater than this <see cref="MaskKeyFrameModelBase"/>'s <see cref="KeyFrameModelBase::FrameNumber"/>.
    /// </param>
    /// <param name="lerpAmount">(In) Value indicating the weight of <paramref name="keyFrameToLerpTo"/>.</param>
    /// <param name="frameDataItem">
    /// (In/Out) A reference to a smart pointer to the <see cref="VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase"/> structure
    /// to be interpolated to.
    /// </param>
    /// <returns>True if the values of the <paramref name="frameDataItem"/> differed from the interpolated values; otherwise, False.</returns>
    virtual bool SetFrameDataItemFromLerpedKeyFrames(const std::shared_ptr<MaskKeyFrameModelBase>& keyFrameToLerpTo, const double lerpAmount, std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>& frameDataItem) = 0;
};

/// <summary>
/// Model encapsulating ellipse masking segment key frame data.
/// </summary>
struct MaskEllipseKeyFrameModel : MaskKeyFrameModelBase
{
    /// <summary>The center point of the ellipse.</summary>
    VideoScriptEditor::Unmanaged::PointD CenterPoint;

    /// <summary>The x-radius value of the ellipse.</summary>
    double RadiusX;

    /// <summary>The y-radius value of the ellipse.</summary>
    double RadiusY;

    /// <summary>
    /// Creates a new <see cref="MaskEllipseKeyFrameModel"/> instance.
    /// </summary>
    /// <param name="frameNumber">The zero-based frame number of the key frame.</param>
    /// <param name="centerPoint">The center point of the ellipse.</param>
    /// <param name="radiusX">The x-radius value of the ellipse.</param>
    /// <param name="radiusY">The y-radius value of the ellipse.</param>
    MaskEllipseKeyFrameModel(int frameNumber, VideoScriptEditor::Unmanaged::PointD centerPoint, double radiusX, double radiusY)
        : MaskKeyFrameModelBase(frameNumber), CenterPoint(centerPoint), RadiusX(radiusX), RadiusY(radiusY)
    {
    }

    ~MaskEllipseKeyFrameModel()
    {
    }

    /// <summary>
    /// Linearly interpolates between this <see cref="MaskEllipseKeyFrameModel"/> and another <see cref="MaskEllipseKeyFrameModel"/>
    /// based on the given weighting and sets the field values of a <see cref="VideoScriptEditor::Unmanaged::MaskEllipseSegmentFrameDataItem"/>
    /// structure only if its field values differ from the interpolated values.
    /// </summary>
    /// <remarks>Overrides <see cref="MaskKeyFrameModelBase::SetFrameDataItemFromLerpedKeyFrames"/>.</remarks>
    /// <param name="keyFrameToLerpTo">
    /// (In) A reference to a smart pointer to a <see cref="MaskEllipseKeyFrameModel"/> with a <see cref="KeyFrameModelBase::FrameNumber"/>
    /// greater than this <see cref="MaskEllipseKeyFrameModel"/>'s <see cref="KeyFrameModelBase::FrameNumber"/>.
    /// </param>
    /// <param name="lerpAmount">(In) Value indicating the weight of <paramref name="keyFrameToLerpTo"/>.</param>
    /// <param name="frameDataItem">
    /// (In/Out) A reference to a smart pointer to the <see cref="VideoScriptEditor::Unmanaged::MaskEllipseSegmentFrameDataItem"/> structure
    /// to be interpolated to.
    /// </param>
    /// <returns>True if the values of the <paramref name="frameDataItem"/> differed from the interpolated values; otherwise, False.</returns>
    bool SetFrameDataItemFromLerpedKeyFrames(const std::shared_ptr<MaskKeyFrameModelBase>& keyFrameToLerpTo, const double lerpAmount, std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>& frameDataItem) override;
};

/// <summary>
/// Model encapsulating polygon masking segment key frame data.
/// </summary>
struct MaskPolygonKeyFrameModel : MaskKeyFrameModelBase
{
    /// <summary>A collection of points that make up the polygon.</summary>
    std::vector<VideoScriptEditor::Unmanaged::PointD> Points;

    /// <summary>
    /// Creates a new <see cref="MaskPolygonKeyFrameModel"/> instance.
    /// </summary>
    /// <param name="frameNumber">The zero-based frame number of the key frame.</param>
    MaskPolygonKeyFrameModel(int frameNumber) : MaskKeyFrameModelBase(frameNumber)
    {
    }

    ~MaskPolygonKeyFrameModel()
    {
    }

    /// <summary>
    /// Linearly interpolates between this <see cref="MaskPolygonKeyFrameModel"/> and another <see cref="MaskPolygonKeyFrameModel"/>
    /// based on the given weighting and sets the field values of a <see cref="VideoScriptEditor::Unmanaged::MaskPolygonSegmentFrameDataItem"/>
    /// structure only if its field values differ from the interpolated values.
    /// </summary>
    /// <remarks>Overrides <see cref="MaskKeyFrameModelBase::SetFrameDataItemFromLerpedKeyFrames"/>.</remarks>
    /// <param name="keyFrameToLerpTo">
    /// (In) A reference to a smart pointer to a <see cref="MaskPolygonKeyFrameModel"/> with a <see cref="KeyFrameModelBase::FrameNumber"/>
    /// greater than this <see cref="MaskPolygonKeyFrameModel"/>'s <see cref="KeyFrameModelBase::FrameNumber"/>.
    /// </param>
    /// <param name="lerpAmount">(In) Value indicating the weight of <paramref name="keyFrameToLerpTo"/>.</param>
    /// <param name="frameDataItem">
    /// (In/Out) A reference to a smart pointer to the <see cref="VideoScriptEditor::Unmanaged::MaskPolygonSegmentFrameDataItem"/> structure
    /// to be interpolated to.
    /// </param>
    /// <returns>True if the values of the <paramref name="frameDataItem"/> differed from the interpolated values; otherwise, False.</returns>
    bool SetFrameDataItemFromLerpedKeyFrames(const std::shared_ptr<MaskKeyFrameModelBase>& keyFrameToLerpTo, const double lerpAmount, std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>& frameDataItem) override;
};

/// <summary>
/// Model encapsulating rectangle masking segment key frame data.
/// </summary>
struct MaskRectangleKeyFrameModel : MaskKeyFrameModelBase
{
    /// <summary>The left pixel coordinate of the rectangle.</summary>
    double Left;

    /// <summary>The top pixel coordinate of the rectangle.</summary>
    double Top;

    /// <summary>The pixel width of the rectangle.</summary>
    double Width;

    /// <summary>The pixel height of the rectangle.</summary>
    double Height;

    /// <summary>
    /// Creates a new <see cref="MaskRectangleKeyFrameModel"/> instance.
    /// </summary>
    /// <param name="frameNumber">The zero-based frame number of the key frame.</param>
    /// <param name="left">The left pixel coordinate of the rectangle.</param>
    /// <param name="top">The top pixel coordinate of the rectangle.</param>
    /// <param name="width">The pixel width of the rectangle.</param>
    /// <param name="height">The pixel height of the rectangle.</param>
    MaskRectangleKeyFrameModel(int frameNumber, double left, double top, double width, double height)
        : MaskKeyFrameModelBase(frameNumber), Left(left), Top(top), Width(width), Height(height)
    {
    }

    ~MaskRectangleKeyFrameModel()
    {
    }

    /// <summary>
    /// Linearly interpolates between this <see cref="MaskRectangleKeyFrameModel"/> and another <see cref="MaskRectangleKeyFrameModel"/>
    /// based on the given weighting and sets the field values of a <see cref="VideoScriptEditor::Unmanaged::MaskRectangleSegmentFrameDataItem"/>
    /// structure only if its field values differ from the interpolated values.
    /// </summary>
    /// <remarks>Overrides <see cref="MaskKeyFrameModelBase::SetFrameDataItemFromLerpedKeyFrames"/>.</remarks>
    /// <param name="keyFrameToLerpTo">
    /// (In) A reference to a smart pointer to a <see cref="MaskRectangleKeyFrameModel"/> with a <see cref="KeyFrameModelBase::FrameNumber"/>
    /// greater than this <see cref="MaskRectangleKeyFrameModel"/>'s <see cref="KeyFrameModelBase::FrameNumber"/>.
    /// </param>
    /// <param name="lerpAmount">(In) Value indicating the weight of <paramref name="keyFrameToLerpTo"/>.</param>
    /// <param name="frameDataItem">
    /// (In/Out) A reference to a smart pointer to the <see cref="VideoScriptEditor::Unmanaged::MaskRectangleSegmentFrameDataItem"/> structure
    /// to be interpolated to.
    /// </param>
    /// <returns>True if the values of the <paramref name="frameDataItem"/> differed from the interpolated values; otherwise, False.</returns>
    bool SetFrameDataItemFromLerpedKeyFrames(const std::shared_ptr<MaskKeyFrameModelBase>& keyFrameToLerpTo, const double lerpAmount, std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>& frameDataItem) override;
};

/// <summary>
/// Encapsulates a Video Script Editor project.
/// </summary>
struct VSEProject
{
    /// <summary>Indicates whether Direct2D processing is required for this project.</summary>
    bool NeedsDirect2DProcessing;

    /// <summary>Video processing options such as video resizing.</summary>
    VideoProcessingOptionsModel VideoProcessingOptions;

    /// <summary>
    /// Collection of <see cref="SegmentModel"/>s in this project.
    /// </summary>
    std::vector<SegmentModel> SegmentModels;

    /// <summary>
    /// Creates a new <see cref="VSEProject"/> instance.
    /// </summary>
    VSEProject()
    {
        NeedsDirect2DProcessing = false;
    }
};