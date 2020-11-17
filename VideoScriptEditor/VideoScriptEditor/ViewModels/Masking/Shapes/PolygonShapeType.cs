using System.ComponentModel;
using VideoScriptEditor.Converters;

namespace VideoScriptEditor.ViewModels.Masking.Shapes
{
    /// <summary>
    /// Describes the geometric type of a polygon masking shape.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum PolygonShapeType
    {
        [Description("Isosceles Triangle")]
        IsoscelesTriangle = 100,

        [Description("Right Triangle")]
        RightTriangle
    }
}