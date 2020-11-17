#pragma once
#include "SoftwareD2DRenderer.h"

/// <summary>
/// Encapsulates rendering data for a single axis-aligned (zero rotation angle) crop.
/// </summary>
struct SingleAxisAlignedCropRenderData
{
    /// <summary>src_left parameter for the AviSynth Spline Resize filter.</summary>
    float SourceLeft;

    /// <summary>src_top parameter for the AviSynth Spline Resize filter.</summary>
    float SourceTop;

    /// <summary>src_width parameter for the AviSynth Spline Resize filter.</summary>
    float SourceWidth;

    /// <summary>src_height parameter for the AviSynth Spline Resize filter.</summary>
    float SourceHeight;

    /// <summary>The number of left and right video frame pixel columns to fill with black.</summary>
    int BorderLeftRight;

    /// <summary>The number of top and bottom video frame pixel rows to fill with black.</summary>
    int BorderTopBottom;
};

/// <summary>
/// AviSynth filter/plugin for processing Video Script Editor projects
/// via AviSynth and a suitable encoding application such as x264.
/// </summary>
class VSEProcessorAviSynth : public GenericVideoFilter
{
    /// <summary>The Video Script Editor project being processed.</summary>
    VSEProject _project;

    /// <summary>The <see cref="SoftwareD2DRenderer"/> instance.</summary>
    std::unique_ptr<SoftwareD2DRenderer> _d2dRenderer;

    /// <summary>The source <see cref="PClip"/> passed to this filter.</summary>
    PClip _sourceClip;

    /// <summary>
    /// The <see cref="_sourceClip"/> converted to RGB and flipped vertically,
    /// ready for input to <see cref="_d2dRenderer"/> functions if Direct2D processing is needed.
    /// </summary>
    PClip _d2dRgbSourceClip;

    /// <summary>
    /// An unsorted collection of zero-based track numbers for masking segments whose frame range includes the current frame number.
    /// </summary>
    std::vector<int> _activeMaskingSegmentTracks;

    /// <summary>
    /// A <see cref="std::map"/> of 'Active' masking segments sorted and keyed by zero-based track number,
    /// providing a <see cref="std::pair"/> association between masking segment frame data and <see cref="ID2D1Geometry"/> objects.
    /// </summary>
    /// <remarks>Active masking segments are those whose frame range includes the current frame number.</remarks>
    std::map<int, std::pair<std::shared_ptr<VideoScriptEditor::Unmanaged::MaskSegmentFrameDataItemBase>, ID2D1GeometryPtr>> _activeMaskingSegments;

    /// <summary>
    /// An unsorted collection of zero-based track numbers for cropping segments whose frame range includes the current frame number.
    /// </summary>
    std::vector<int> _activeCroppingSegmentTracks;

    /// <summary>
    /// A <see cref="std::map"/> of 'Active' cropping segments sorted and keyed by zero-based track number.
    /// </summary>
    /// <remarks>Active cropping segments are those whose frame range includes the current frame number.</remarks>
    std::map<int, VideoScriptEditor::Unmanaged::CropSegmentFrameDataItem> _activeCroppingSegments;

public:
    /// <summary>
    /// Creates a new <see cref="VSEProcessorAviSynth"/> instance.
    /// </summary>
    /// <param name="childClip">The child (source) clip.</param>
    /// <param name="projectFileName">The file path of the Video Script Editor project to process.</param>
    /// <param name="env">The AviSynth <see cref="IScriptEnvironment"/> interface.</param>
    VSEProcessorAviSynth(PClip childClip, const char* projectFileName, IScriptEnvironment* env);

    /// <summary>Destructor.</summary>
    ~VSEProcessorAviSynth() {}

    /// <summary>
    /// Called when AviSynth requests frame <paramref name="n"/> from this filter.
    /// </summary>
    /// <param name="n">The requested frame number.</param>
    /// <param name="env">The AviSynth <see cref="IScriptEnvironment"/> interface.</param>
    /// <returns>The requested frame.</returns>
    PVideoFrame __stdcall GetFrame(int n, IScriptEnvironment* env);

    /// <summary>
    /// AviSynth callback function for creating a new instance of this filter.
    /// </summary>
    /// <param name="args">An <see cref="AVSValue"/> containing an array of filter arguments.</param>
    /// <param name="user_data">The user_data cookie.</param>
    /// <param name="env">The AviSynth <see cref="IScriptEnvironment"/> interface.</param>
    /// <returns>An <see cref="AVSValue"/> containing a new instance of this filter as a clip.</returns>
    static AVSValue __cdecl Create(AVSValue args, void* user_data, IScriptEnvironment* env);

private:
    /// <summary>
    /// Returns a <see cref="PClip"/> with a blur mask effect
    /// overlaid on the current frame of the <paramref name="overlaySourceClip"/> at a given offset.
    /// </summary>
    /// <param name="maskGeometryOffset">
    /// A reference to a <see cref="POINT"/> specifying the horizontal and vertical amount to offset the geometric mask overlay.
    /// </param>
    /// <param name="overlaySourceClip">A reference to the source <see cref="PClip"/> for the Overlay filter.</param>
    /// <param name="frameNumber">The current frame number.</param>
    /// <param name="env">The AviSynth <see cref="IScriptEnvironment"/> interface.</param>
    /// <returns>
    /// A <see cref="PClip"/> with a blur mask effect overlaid on the current frame of the <paramref name="overlaySourceClip"/>
    /// at the specified offset.
    /// </returns>
    PClip ApplyBlurMask(const POINT& maskGeometryOffset, const PClip& overlaySourceClip, const int frameNumber, IScriptEnvironment* env);

    /// <summary>
    /// Processes the <see cref="_activeMaskingSegments"/> and rotated/multiple <see cref="_activeCroppingSegments"/>
    /// using the <see cref="_d2dRenderer"/>.
    /// </summary>
    /// <param name="frameNumber">The current frame number.</param>
    /// <param name="env">The AviSynth <see cref="IScriptEnvironment"/> interface.</param>
    /// <returns>A <see cref="PVideoFrame"/> containing Direct2D rendered content, converted to YV12.</returns>
    PVideoFrame ProcessActiveSegmentsUsingDirect2D(const int frameNumber, IScriptEnvironment* env);

    /// <summary>
    /// Applies a single axis-aligned (zero rotation angle) crop using the <paramref name="cropSegmentFrameData"/>
    /// to the current frame of the <paramref name="croppingSourceClip"/> at the specified <paramref name="cropSegmentFrameOffset"/>.
    /// </summary>
    /// <remarks>
    /// This type of crop is able to be performed through AviSynth APIs, avoiding the RGB color conversion penalty
    /// that occurs when rendering the more complex rotated/multiple segment crops through Direct2D.
    /// </remarks>
    /// <param name="croppingSourceClip">A reference to the source <see cref="PClip"/>.</param>
    /// <param name="cropSegmentFrameData">
    /// A reference to a <see cref="VideoScriptEditor::Unmanaged::CropSegmentFrameDataItem"/>
    /// containing the cropping data.
    /// </param>
    /// <param name="cropSegmentFrameOffset">
    /// A reference to a <see cref="POINT"/> specifying the horizontal and vertical amount to offset the cropped content.
    /// </param>
    /// <param name="frameNumber">The current frame number.</param>
    /// <param name="env">The AviSynth <see cref="IScriptEnvironment"/> interface.</param>
    /// <returns>The resulting <see cref="PVideoFrame"/>.</returns>
    PVideoFrame ApplySingleAxisAlignedCrop(const PClip& croppingSourceClip, const VideoScriptEditor::Unmanaged::CropSegmentFrameDataItem& cropSegmentFrameData, const POINT& cropSegmentFrameOffset, const int frameNumber, IScriptEnvironment* env);

    /// <summary>
    /// Calculates the render data for a single axis-aligned (zero rotation angle) crop.
    /// </summary>
    /// <param name="cropSegmentFrameData">
    /// A reference to a <see cref="VideoScriptEditor::Unmanaged::CropSegmentFrameDataItem"/>
    /// containing the cropping data.
    /// </param>
    /// <param name="cropSegmentFrameOffset">
    /// A reference to a <see cref="POINT"/> specifying the horizontal and vertical amount to offset
    /// the <paramref name="cropSegmentFrameData"/>.
    /// </param>
    /// <param name="env">The AviSynth <see cref="IScriptEnvironment"/> interface.</param>
    /// <returns>
    /// A <see cref="SingleAxisAlignedCropRenderData"/> structure containing the calculated render data
    /// for a single axis-aligned (zero rotation angle) crop.
    /// </returns>
    SingleAxisAlignedCropRenderData CalculateRenderDataForSingleAxisAlignedCrop(const VideoScriptEditor::Unmanaged::CropSegmentFrameDataItem& cropSegmentFrameData, const POINT& cropSegmentFrameOffset, IScriptEnvironment* env);

    /// <summary>
    /// Fills an even number of YV12 video frame pixel rows or columns with black,
    /// creating a letterboxed border.
    /// </summary>
    /// <remarks>
    /// <paramref name="borderLeftRight"/> and <paramref name="borderTopBottom"/>
    /// values must be mod2 (divisible by 2) for correct YV12 color alignment.
    /// </remarks>
    /// <param name="videoFrame">(IN/OUT) A reference to the <see cref="PVideoFrame"/> to fill borders.</param>
    /// <param name="videoFrameInfo">
    /// (IN) A reference to a <see cref="VideoInfo"/> structure containing the width and height of the <paramref name="videoFrame"/>.
    /// </param>
    /// <param name="borderLeftRight">(IN) The number of left and right video frame pixel columns to fill with black.</param>
    /// <param name="borderTopBottom">(IN) The number of top and bottom video frame pixel rows to fill with black.</param>
    /// <param name="env">(IN) The AviSynth <see cref="IScriptEnvironment"/> interface.</param>
    void FillYV12Borders(PVideoFrame& videoFrame, const VideoInfo& videoFrameInfo, const int borderLeftRight, const int borderTopBottom, IScriptEnvironment* env);

    /// <summary>
    /// Overlays an odd number of video frame pixel rows or columns with black via the AviSynth Overlay filter,
    /// creating a letterboxed border.
    /// </summary>
    /// <remarks>
    /// For when <paramref name="borderLeftRight"/> and <paramref name="borderTopBottom"/>
    /// values aren't mod2 (divisible by 2) and chroma subsampling is required for correct YV12 color alignment.
    /// </remarks>
    /// <param name="sourceClip">A reference to the source <see cref="PClip"/>.</param>
    /// <param name="borderLeftRight">The number of left and right video frame pixel columns to overlay with black.</param>
    /// <param name="borderTopBottom">The number of top and bottom video frame pixel rows to overlay with black.</param>
    /// <param name="env">The AviSynth <see cref="IScriptEnvironment"/> interface.</param>
    /// <returns>The overlaid <see cref="PClip"/>.</returns>
    PClip OverlayBorders(const PClip& sourceClip, const int borderLeftRight, const int borderTopBottom, IScriptEnvironment* env);

    /// <summary>
    /// Invokes the AviSynth Overlay filter with the required base and overlay clip
    /// and optional overlay x-offset, y-offset and mask clip parameters.
    /// See http://avisynth.nl/index.php/Overlay for more information.
    /// </summary>
    /// <param name="env">The AviSynth <see cref="IScriptEnvironment"/> interface.</param>
    /// <param name="baseClip">The required 'clip' parameter value.</param>
    /// <param name="overlayClip">The required 'overlay' parameter value.</param>
    /// <param name="overlayOffsetX">The optional 'x' parameter value.</param>
    /// <param name="overlayOffsetY">The optional 'y' parameter value.</param>
    /// <param name="maskClip">The optional 'mask' parameter value.</param>
    /// <returns>The overlaid <see cref="PClip"/>.</returns>
    PClip InvokeAvsOverlayFilter(IScriptEnvironment* env, const PClip& baseClip, const PClip& overlayClip, const int overlayOffsetX = 0, const int overlayOffsetY = 0, const AVSValue maskClip = AVSValue());

    /// <summary>
    /// Invokes an AviSynth color conversion filter (see http://avisynth.nl/index.php/Convert)
    /// with conversion matrix selection determined by the height of the <paramref name="sourceClip"/>.
    /// </summary>
    /// <param name="env">The AviSynth <see cref="IScriptEnvironment"/> interface.</param>
    /// <param name="colorConversionFilterName">
    /// The name of the AviSynth filter to invoke.
    /// One of the 'ConvertTo' filters described at http://avisynth.nl/index.php/Convert
    /// </param>
    /// <param name="sourceClip">The source <see cref="PClip"/> to be passed to the filter.</param>
    /// <returns>The resulting color converted <see cref="PClip"/>.</returns>
    PClip InvokeAvsColorConversionFilter(IScriptEnvironment* env, const char* colorConversionFilterName, const PClip& sourceClip);

    /// <summary>
    /// Invokes an AviSynth filter, passing in the specified named arguments.
    /// </summary>
    /// <param name="env">The AviSynth <see cref="IScriptEnvironment"/> interface.</param>
    /// <param name="filterName">The name of the AviSynth filter to invoke.</param>
    /// <param name="filterArgs">An <see cref="AVSValue"/> containing an array of arguments to pass to the filter.</param>
    /// <param name="filterArgNames">An optional array containing the name of each argument in the <paramref name="filterArgs"/>.</param>
    /// <returns>The <see cref="PClip"/> returned from the filter.</returns>
    PClip InvokeAvsFilter(IScriptEnvironment* env, const char* filterName, const AVSValue filterArgs, const char* filterArgNames[] = nullptr);
};