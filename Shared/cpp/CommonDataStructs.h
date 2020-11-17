#pragma once

namespace VideoScriptEditor::Unmanaged
{
    /// <summary>
    /// Encapsulates cropping segment frame data.
    /// </summary>
    struct CropSegmentFrameDataItem
    {
        /// <summary>
        /// The left pixel coordinate of the area to crop.
        /// </summary>
        double Left;

        /// <summary>
        /// The top pixel coordinate of the area to crop.
        /// </summary>
        double Top;

        /// <summary>
        /// The pixel width of the area to crop.
        /// </summary>
        double Width;

        /// <summary>
        /// The pixel height of the area to crop.
        /// </summary>
        double Height;

        /// <summary>
        /// The angle in degrees at which the crop area is rotated.
        /// </summary>
        double Angle;

        /// <summary>
        /// Creates a new <see cref="CropSegmentFrameDataItem"/> instance.
        /// </summary>
        /// <param name="left">The left pixel coordinate of the area to crop.</param>
        /// <param name="top">The top pixel coordinate of the area to crop.</param>
        /// <param name="width">The pixel width of the area to crop.</param>
        /// <param name="height">The pixel height of the area to crop.</param>
        /// <param name="angle">The angle in degrees at which the crop area is rotated.</param>
        CropSegmentFrameDataItem(double left, double top, double width, double height, double angle)
            : Left(left), Top(top), Width(width), Height(height), Angle(angle)
        {
        }

        /// <summary>
        /// Creates a new <see cref="CropSegmentFrameDataItem"/> instance with default values.
        /// </summary>
        CropSegmentFrameDataItem() = default;

        /// <summary>
        /// Default destructor for the <see cref="CropSegmentFrameDataItem"/>.
        /// </summary>
        ~CropSegmentFrameDataItem() = default;
    };

    /// <summary>
    /// Base class for masking segment frame data items.
    /// </summary>
    class MaskSegmentFrameDataItemBase
    {
    protected:
        /// <summary>
        /// Base constructor for classes derived from the <see cref="MaskSegmentFrameDataItemBase"/> class.
        /// </summary>
        MaskSegmentFrameDataItemBase() = default;
    public:
        /// <summary>
        /// Base destructor for classes derived from the <see cref="MaskSegmentFrameDataItemBase"/> class.
        /// </summary>
        virtual ~MaskSegmentFrameDataItemBase() = default;
    };

    /// <summary>
    /// Encapsulates ellipse masking segment frame data.
    /// </summary>
    struct MaskEllipseSegmentFrameDataItem : public MaskSegmentFrameDataItemBase
    {
        /// <summary>
        /// The center point of the ellipse.
        /// </summary>
        PointD CenterPoint;

        /// <summary>
        /// The x-radius value of the ellipse.
        /// </summary>
        double RadiusX;

        /// <summary>
        /// The y-radius value of the ellipse.
        /// </summary>
        double RadiusY;

        /// <summary>
        /// Creates a new <see cref="MaskEllipseSegmentFrameDataItem"/> instance.
        /// </summary>
        /// <param name="centerPoint">The center point of the ellipse.</param>
        /// <param name="radiusX">The x-radius value of the ellipse.</param>
        /// <param name="radiusY">The y-radius value of the ellipse.</param>
        MaskEllipseSegmentFrameDataItem(PointD centerPoint, double radiusX, double radiusY)
            : CenterPoint(centerPoint), RadiusX(radiusX), RadiusY(radiusY)
        {
        }

        /// <summary>
        /// Creates a new <see cref="MaskEllipseSegmentFrameDataItem"/> instance with default values.
        /// </summary>
        MaskEllipseSegmentFrameDataItem() = default;

        /// <summary>
        /// Default destructor for the <see cref="MaskEllipseSegmentFrameDataItem"/>.
        /// </summary>
        ~MaskEllipseSegmentFrameDataItem() = default;
    };

    /// <summary>
    /// Encapsulates polygon masking segment frame data.
    /// </summary>
    struct MaskPolygonSegmentFrameDataItem : public MaskSegmentFrameDataItemBase
    {
        /// <summary>
        /// A collection of points that make up the polygon.
        /// </summary>
        std::vector<PointD> Points;

        /// <summary>
        /// Creates a new <see cref="MaskPolygonSegmentFrameDataItem"/> instance.
        /// </summary>
        /// <param name="points">A collection of points that make up the polygon.</param>
        MaskPolygonSegmentFrameDataItem(std::vector<PointD>&& points)
            : Points(points)
        {
        }

        /// <summary>
        /// Creates a new <see cref="MaskPolygonSegmentFrameDataItem"/> instance with default values.
        /// </summary>
        MaskPolygonSegmentFrameDataItem() = default;


        /// <summary>
        /// Destructor for the <see cref="MaskPolygonSegmentFrameDataItem"/>.
        /// </summary>
        ~MaskPolygonSegmentFrameDataItem()
        {
        }
    };

    /// <summary>
    /// Encapsulates rectangle masking segment frame data.
    /// </summary>
    struct MaskRectangleSegmentFrameDataItem : public MaskSegmentFrameDataItemBase
    {
        /// <summary>
        /// The left pixel coordinate of the rectangle.
        /// </summary>
        double Left;

        /// <summary>
        /// The top pixel coordinate of the rectangle.
        /// </summary>
        double Top;

        /// <summary>
        /// The pixel width of the rectangle.
        /// </summary>
        double Width;

        /// <summary>
        /// The pixel height of the rectangle.
        /// </summary>
        double Height;

        /// <summary>
        /// Creates a new <see cref="MaskPolygonSegmentFrameDataItem"/> instance.
        /// </summary>
        /// <param name="left">The left pixel coordinate of the rectangle.</param>
        /// <param name="top">The top pixel coordinate of the rectangle.</param>
        /// <param name="width">The pixel width of the rectangle.</param>
        /// <param name="height">The pixel height of the rectangle.</param>
        MaskRectangleSegmentFrameDataItem(double left, double top, double width, double height)
            : Left(left), Top(top), Width(width), Height(height)
        {
        }

        /// <summary>
        /// Creates a new <see cref="MaskRectangleSegmentFrameDataItem"/> instance with default values.
        /// </summary>
        MaskRectangleSegmentFrameDataItem() = default;

        /// <summary>
        /// Default destructor for the <see cref="MaskRectangleSegmentFrameDataItem"/>.
        /// </summary>
        ~MaskRectangleSegmentFrameDataItem() = default;
    };

    /// <summary>
    /// Encapsulates cropping segment frame rendering data.
    /// </summary>
    struct CropSegmentFrameRenderItem
    {
        /// <summary>
        /// The scale factor of the scale transformation.
        /// </summary>
        float ScaleFactor;

        /// <summary>
        /// The calculated render size after performing the scale transformation.
        /// </summary>
        D2D1_SIZE_F ScaledSize;

        /// <summary>
        /// The rotation angle in degrees.
        /// </summary>
        float RotationAngle;

        /// <summary>
        /// The point about which the rotation is performed.
        /// </summary>
        D2D1_POINT_2F RotationCenter;

        /// <summary>
        /// The distance to translate along the x-axis.
        /// </summary>
        float TranslationOffsetX;

        /// <summary>
        /// The distance to translate along the y-axis.
        /// </summary>
        float TranslationOffsetY;
    };
}