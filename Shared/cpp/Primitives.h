#pragma once

namespace VideoScriptEditor::Unmanaged
{
    /// <summary>
    /// Represents a double-precision x-coordinate and y-coordinate pair in two-dimensional space.
    /// </summary>
    struct PointD
    {
        /// <summary>
        /// The x-coordinate of this <see cref="PointD"/>.
        /// </summary>
        double X;

        /// <summary>
        /// The y-coordinate of this <see cref="PointD"/>.
        /// </summary>
        double Y;

        /// <summary>
        /// Creates a new <see cref="PointD"/> instance from the specified coordinates.
        /// </summary>
        /// <param name="x">The x-coordinate. Defaults to zero.</param>
        /// <param name="y">The y-coordinate. Defaults to zero.</param>
        PointD(double x = 0.0, double y = 0.0)
            : X(x), Y(y)
        {
        }

        /// <summary>
        /// Compares two <see cref="PointD"/> instances for exact equality.
        /// </summary>
        /// <param name="lhs">The left hand side <see cref="PointD"/> instance to compare.</param>
        /// <param name="rhs">The right hand side <see cref="PointD"/> instance to compare.</param>
        /// <returns>True if the two <see cref="PointD"/> instances are exactly equal, False otherwise.</returns>
        friend bool operator==(const PointD& lhs, const PointD& rhs)
        {
            return lhs.X == rhs.X
                && lhs.Y == rhs.Y;
        }

        /// <summary>
        /// Compares two <see cref="PointD"/> instances for exact inequality.
        /// </summary>
        /// <param name="lhs">The left hand side <see cref="PointD"/> instance to compare.</param>
        /// <param name="rhs">The right hand side <see cref="PointD"/> instance to compare.</param>
        /// <returns>True if the two <see cref="PointD"/> instances are exactly unequal, False otherwise.</returns>
        friend bool operator!=(const PointD& lhs, const PointD& rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Explicitly converts the <see cref="PointD"/> to a <see cref="D2D1_POINT_2F"/>.
        /// </summary>
        explicit operator D2D1_POINT_2F() const
        {
            return {
                static_cast<FLOAT>(X),
                static_cast<FLOAT>(Y)
            };
        }
    };

    /// <summary>
    /// Represents the size of a rectangular region with an ordered pair of double-precision width and height values.
    /// </summary>
    struct SizeD
    {
        /// <summary>
        /// Represents the horizontal component of this <see cref="SizeD"/>.
        /// </summary>
        double Width;

        /// <summary>
        /// Represents the vertical component of this <see cref="SizeD"/>.
        /// </summary>
        double Height;

        /// <summary>
        /// Creates a new <see cref="SizeD"/> instance from the specified dimensions.
        /// </summary>
        /// <param name="width">The horizontal dimension. Defaults to zero.</param>
        /// <param name="height">The vertical dimension. Defaults to zero.</param>
        SizeD(double width = 0.0, double height = 0.0)
            : Width(width), Height(height)
        {
        }
    };

    /// <summary>
    /// Represents a rectangle defined by double-precision left-coordinate, top-coordinate, width and height.
    /// </summary>
    struct LtwhRectD
    {
        /// <summary>
        /// The left coordinate of the rectangle.
        /// </summary>
        double Left;

        /// <summary>
        /// The top coordinate of the rectangle.
        /// </summary>
        double Top;

        /// <summary>
        /// The width of the rectangle.
        /// </summary>
        double Width;

        /// <summary>
        /// The height of the rectangle.
        /// </summary>
        double Height;

        /// <summary>
        /// Creates a new <see cref="LtwhRectD"/> instance from the specified top-left coordinate and size dimensions.
        /// </summary>
        /// <param name="left">The left coordinate of the rectangle. Defaults to zero.</param>
        /// <param name="top">The top coordinate of the rectangle. Defaults to zero.</param>
        /// <param name="width">The width of the rectangle. Defaults to zero.</param>
        /// <param name="height">The height of the rectangle. Defaults to zero.</param>
        LtwhRectD(double left = 0.0, double top = 0.0, double width = 0.0, double height = 0.0)
            : Left(left), Top(top), Width(width), Height(height)
        {
        }
    };

    /// <summary>
    /// Represents a rational fraction with a numerator and denominator.
    /// </summary>
    struct Ratio
    {
        /// <summary>
        /// The numerator. A number to be divided by the <see cref="Denominator"/>.
        /// </summary>
        unsigned int Numerator;

        /// <summary>
        /// The denominator. A number that functions as the divisor of the <see cref="Numerator"/>.
        /// </summary>
        unsigned int Denominator;

        /// <summary>
        /// Creates a new <see cref="Ratio"/>.
        /// </summary>
        /// <param name="numerator">The numerator of the <see cref="Ratio"/>. Defaults to zero.</param>
        /// <param name="denominator">The denominator of the <see cref="Ratio"/>. Defaults to one.</param>
        Ratio(unsigned int numerator = 0, unsigned int denominator = 1)
            : Numerator(numerator), Denominator(denominator)
        {
        }
    };
}