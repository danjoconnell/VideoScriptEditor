#pragma once

/// <summary>
/// Mathematical helper functions.
/// </summary>
namespace MathHelpers
{
    /// <summary>
    /// Rounds a double-precision floating-point value to the nearest even integral number.
    /// </summary>
    /// <remarks>
    /// Based on sample code posted by Adam Wright on Stack Overflow at https://stackoverflow.com/questions/4360348/how-to-find-nearest-even-number-for-given-int-given-11-return-12/22894257#22894257
    /// </remarks>
    /// <param name="value">The double-precision floating-point number to be rounded.</param>
    /// <returns>The even integral number nearest to <paramref name="value"/>.</returns>
    inline long RoundToNearestEvenIntegral(const double value)
    {
        return lround(value * 0.5) * 2;
    }

    /// <summary>
    /// Expands a <see cref="D2D1_SIZE_U"/> to the nearest even integral dimensions that satisfy the specified aspect ratio.
    /// </summary>
    /// <param name="size">A reference to the source <see cref="D2D1_SIZE_U"/> structure.</param>
    /// <param name="aspectRatio">A reference to the target <see cref="VideoScriptEditor::Unmanaged::Ratio">aspect ratio</see> for expansion.</param>
    /// <returns>An expanded <see cref="D2D1_SIZE_U"/> with even integral dimensions that satisfy the specified aspect ratio.</returns>
    inline D2D1_SIZE_U ExpandToAspectRatio(const D2D1_SIZE_U& size, const VideoScriptEditor::Unmanaged::Ratio& aspectRatio)
    {
        double outputWidth, outputHeight;

        // resize using original width
        outputWidth = static_cast<double>(size.width);
        outputHeight = outputWidth * (static_cast<double>(aspectRatio.Denominator) / static_cast<double>(aspectRatio.Numerator));

        if (outputHeight < static_cast<double>(size.height))
        {
            // resize using original height
            outputHeight = static_cast<double>(size.height);
            outputWidth = outputHeight * (static_cast<double>(aspectRatio.Numerator) / static_cast<double>(aspectRatio.Denominator));
        }

        return D2D1::SizeU(
            RoundToNearestEvenIntegral(outputWidth),
            RoundToNearestEvenIntegral(outputHeight)
        );
    }

    /// <summary>
    /// Linearly interpolates between two <see cref="VideoScriptEditor::Unmanaged::PointD">points</see> based on the given weighting.
    /// </summary>
    /// <param name="firstPoint">A reference to the first source <see cref="VideoScriptEditor::Unmanaged::PointD">point</see>.</param>
    /// <param name="secondPoint">A reference to the second source <see cref="VideoScriptEditor::Unmanaged::PointD">point</see>.</param>
    /// <param name="amount">Value indicating the weight of the second source <see cref="VideoScriptEditor::Unmanaged::PointD">point</see>.</param>
    /// <returns>The interpolated <see cref="VideoScriptEditor::Unmanaged::PointD">point</see>.</returns>
    inline VideoScriptEditor::Unmanaged::PointD Lerp(const VideoScriptEditor::Unmanaged::PointD& firstPoint, const VideoScriptEditor::Unmanaged::PointD& secondPoint, const double amount)
    {
        return VideoScriptEditor::Unmanaged::PointD(
            std::lerp(firstPoint.X, secondPoint.X, amount),
            std::lerp(firstPoint.Y, secondPoint.Y, amount)
        );
    }
}