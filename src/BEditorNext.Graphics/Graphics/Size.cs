﻿using System.Globalization;
using System.Numerics;
using System.Text.Json.Serialization;

using BEditorNext.Converters;
using BEditorNext.Utilities;

namespace BEditorNext.Graphics;

/// <summary>
/// Defines a size.
/// </summary>
[JsonConverter(typeof(SizeJsonConverter))]
public readonly struct Size : IEquatable<Size>
{
    /// <summary>
    /// A size representing infinity.
    /// </summary>
    public static readonly Size Infinity = new(float.PositiveInfinity, float.PositiveInfinity);

    /// <summary>
    /// A size representing zero
    /// </summary>
    public static readonly Size Empty = new(0, 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="Size"/> structure.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public Size(float width, float height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets the aspect ratio of the size.
    /// </summary>
    public float AspectRatio => Width / Height;

    /// <summary>
    /// Gets the width.
    /// </summary>
    public float Width { get; }

    /// <summary>
    /// Gets the height.
    /// </summary>
    public float Height { get; }

    /// <summary>
    /// Gets a value indicating whether the Width and Height values are zero.
    /// </summary>
    public bool IsDefault => (Width == 0) && (Height == 0);

    /// <summary>
    /// Checks for equality between two <see cref="Size"/>s.
    /// </summary>
    /// <param name="left">The first size.</param>
    /// <param name="right">The second size.</param>
    /// <returns>True if the sizes are equal; otherwise false.</returns>
    public static bool operator ==(Size left, Size right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Checks for inequality between two <see cref="Size"/>s.
    /// </summary>
    /// <param name="left">The first size.</param>
    /// <param name="right">The second size.</param>
    /// <returns>True if the sizes are unequal; otherwise false.</returns>
    public static bool operator !=(Size left, Size right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Scales a size.
    /// </summary>
    /// <param name="size">The size</param>
    /// <param name="scale">The scaling factor.</param>
    /// <returns>The scaled size.</returns>
    public static Size operator *(Size size, Vector2 scale)
    {
        return new Size(size.Width * scale.X, size.Height * scale.Y);
    }

    /// <summary>
    /// Scales a size.
    /// </summary>
    /// <param name="size">The size</param>
    /// <param name="scale">The scaling factor.</param>
    /// <returns>The scaled size.</returns>
    public static Size operator /(Size size, Vector2 scale)
    {
        return new Size(size.Width / scale.X, size.Height / scale.Y);
    }

    /// <summary>
    /// Divides a size by another size to produce a scaling factor.
    /// </summary>
    /// <param name="left">The first size</param>
    /// <param name="right">The second size.</param>
    /// <returns>The scaled size.</returns>
    public static Vector2 operator /(Size left, Size right)
    {
        return new Vector2(left.Width / right.Width, left.Height / right.Height);
    }

    /// <summary>
    /// Scales a size.
    /// </summary>
    /// <param name="size">The size</param>
    /// <param name="scale">The scaling factor.</param>
    /// <returns>The scaled size.</returns>
    public static Size operator *(Size size, float scale)
    {
        return new Size(size.Width * scale, size.Height * scale);
    }

    /// <summary>
    /// Scales a size.
    /// </summary>
    /// <param name="size">The size</param>
    /// <param name="scale">The scaling factor.</param>
    /// <returns>The scaled size.</returns>
    public static Size operator /(Size size, float scale)
    {
        return new Size(size.Width / scale, size.Height / scale);
    }

    public static Size operator +(Size size, Size toAdd)
    {
        return new Size(size.Width + toAdd.Width, size.Height + toAdd.Height);
    }

    public static Size operator -(Size size, Size toSubtract)
    {
        return new Size(size.Width - toSubtract.Width, size.Height - toSubtract.Height);
    }

    /// <summary>
    /// Parses a <see cref="Size"/> string.
    /// </summary>
    /// <param name="s">The string.</param>
    /// <returns>The <see cref="Size"/>.</returns>
    public static Size Parse(string s)
    {
        using var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage: "Invalid Size.");
        return new Size(
            tokenizer.ReadSingle(),
            tokenizer.ReadSingle());
    }

    /// <summary>
    /// Constrains the size.
    /// </summary>
    /// <param name="constraint">The size to constrain to.</param>
    /// <returns>The constrained size.</returns>
    public Size Constrain(Size constraint)
    {
        return new Size(
            MathF.Min(Width, constraint.Width),
            MathF.Min(Height, constraint.Height));
    }

    /// <summary>
    /// Deflates the size by a <see cref="Thickness"/>.
    /// </summary>
    /// <param name="thickness">The thickness.</param>
    /// <returns>The deflated size.</returns>
    /// <remarks>The deflated size cannot be less than 0.</remarks>
    public Size Deflate(Thickness thickness)
    {
        return new Size(
            MathF.Max(0, Width - thickness.Left - thickness.Right),
            MathF.Max(0, Height - thickness.Top - thickness.Bottom));
    }

    /// <summary>
    /// Returns a boolean indicating whether the size is equal to the other given size (bitwise).
    /// </summary>
    /// <param name="other">The other size to test equality against.</param>
    /// <returns>True if this size is equal to other; False otherwise.</returns>
    public bool Equals(Size other)
    {
        return Width == other.Width &&
               Height == other.Height;
    }

    /// <summary>
    /// Returns a boolean indicating whether the size is equal to the other given size (numerically).
    /// </summary>
    /// <param name="other">The other size to test equality against.</param>
    /// <returns>True if this size is equal to other; False otherwise.</returns>
    public bool NearlyEquals(Size other)
    {
        static bool AreClose(float value1, float value2)
        {
            const float FloatEpsilon = 1.192092896e-07F;

            if (value1 == value2) return true;
            float eps = (MathF.Abs(value1) + MathF.Abs(value2) + 10.0f) * FloatEpsilon;
            float delta = value1 - value2;
            return (-eps < delta) && (eps > delta);
        }

        return AreClose(Width, other.Width) &&
               AreClose(Height, other.Height);
    }

    /// <summary>
    /// Checks for equality between a size and an object.
    /// </summary>
    /// <param name="obj">The object.</param>
    /// <returns>
    /// True if <paramref name="obj"/> is a size that equals the current size.
    /// </returns>
    public override bool Equals(object? obj)
    {
        return obj is Size other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for a <see cref="Size"/>.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    /// <summary>
    /// Inflates the size by a <see cref="Thickness"/>.
    /// </summary>
    /// <param name="thickness">The thickness.</param>
    /// <returns>The inflated size.</returns>
    public Size Inflate(Thickness thickness)
    {
        return new Size(
            Width + thickness.Left + thickness.Right,
            Height + thickness.Top + thickness.Bottom);
    }

    /// <summary>
    /// Returns a new <see cref="Size"/> with the same height and the specified width.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <returns>The new <see cref="Size"/>.</returns>
    public Size WithWidth(float width)
    {
        return new Size(width, Height);
    }

    /// <summary>
    /// Returns a new <see cref="Size"/> with the same width and the specified height.
    /// </summary>
    /// <param name="height">The height.</param>
    /// <returns>The new <see cref="Size"/>.</returns>
    public Size WithHeight(float height)
    {
        return new Size(Width, height);
    }

    /// <summary>
    /// Returns the string representation of the size.
    /// </summary>
    /// <returns>The string representation of the size.</returns>
    public override string ToString()
    {
        return FormattableString.Invariant($"{Width}, {Height}");
    }

    /// <summary>
    /// Deconstructs the size into its Width and Height values.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public void Deconstruct(out float width, out float height)
    {
        width = Width;
        height = Height;
    }
}
