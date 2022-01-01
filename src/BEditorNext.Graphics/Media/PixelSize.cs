﻿using System.Globalization;
using System.Numerics;
using System.Text.Json.Serialization;

using BEditorNext.Converters;
using BEditorNext.Graphics;
using BEditorNext.Utilities;

namespace BEditorNext.Media;

/// <summary>
/// Represents a size in device pixels.
/// </summary>
[JsonConverter(typeof(PixelSizeJsonConverter))]
public readonly struct PixelSize : IEquatable<PixelSize>
{
    /// <summary>
    /// A size representing zero
    /// </summary>
    public static readonly PixelSize Empty = new(0, 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="PixelSize"/> structure.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public PixelSize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets the aspect ratio of the size.
    /// </summary>
    public float AspectRatio => (float)Width / Height;

    /// <summary>
    /// Gets the width.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Checks for equality between two <see cref="PixelSize"/>s.
    /// </summary>
    /// <param name="left">The first size.</param>
    /// <param name="right">The second size.</param>
    /// <returns>True if the sizes are equal; otherwise false.</returns>
    public static bool operator ==(PixelSize left, PixelSize right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Checks for inequality between two <see cref="Size"/>s.
    /// </summary>
    /// <param name="left">The first size.</param>
    /// <param name="right">The second size.</param>
    /// <returns>True if the sizes are unequal; otherwise false.</returns>
    public static bool operator !=(PixelSize left, PixelSize right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Parses a <see cref="PixelSize"/> string.
    /// </summary>
    /// <param name="s">The string.</param>
    /// <returns>The <see cref="PixelSize"/>.</returns>
    public static PixelSize Parse(string s)
    {
        using var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage: "Invalid PixelSize.");

        return new PixelSize(
            tokenizer.ReadInt32(),
            tokenizer.ReadInt32());
    }

    /// <summary>
    /// Returns a boolean indicating whether the size is equal to the other given size.
    /// </summary>
    /// <param name="other">The other size to test equality against.</param>
    /// <returns>True if this size is equal to other; False otherwise.</returns>
    public bool Equals(PixelSize other)
    {
        return Width == other.Width && Height == other.Height;
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
        return obj is PixelSize other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for a <see cref="PixelSize"/>.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    /// <summary>
    /// Returns a new <see cref="PixelSize"/> with the same height and the specified width.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <returns>The new <see cref="PixelSize"/>.</returns>
    public PixelSize WithWidth(int width)
    {
        return new PixelSize(width, Height);
    }

    /// <summary>
    /// Returns a new <see cref="PixelSize"/> with the same width and the specified height.
    /// </summary>
    /// <param name="height">The height.</param>
    /// <returns>The new <see cref="PixelSize"/>.</returns>
    public PixelSize WithHeight(int height)
    {
        return new PixelSize(Width, height);
    }

    /// <summary>
    /// Converts the <see cref="PixelSize"/> to a device-independent <see cref="Size"/> using the
    /// specified scaling factor.
    /// </summary>
    /// <param name="scale">The scaling factor.</param>
    /// <returns>The device-independent size.</returns>
    public Size ToSize(float scale)
    {
        return new Size(Width / scale, Height / scale);
    }

    /// <summary>
    /// Converts the <see cref="PixelSize"/> to a device-independent <see cref="Size"/> using the
    /// specified scaling factor.
    /// </summary>
    /// <param name="scale">The scaling factor.</param>
    /// <returns>The device-independent size.</returns>
    public Size ToSize(Vector2 scale)
    {
        return new Size(Width / scale.X, Height / scale.Y);
    }

    /// <summary>
    /// Converts a <see cref="Size"/> to device pixels using the specified scaling factor.
    /// </summary>
    /// <param name="size">The size.</param>
    /// <param name="scale">The scaling factor.</param>
    /// <returns>The device-independent size.</returns>
    public static PixelSize FromSize(Size size, float scale)
    {
        return new PixelSize(
            (int)Math.Ceiling(size.Width * scale),
            (int)Math.Ceiling(size.Height * scale));
    }

    /// <summary>
    /// Converts a <see cref="Size"/> to device pixels using the specified scaling factor.
    /// </summary>
    /// <param name="size">The size.</param>
    /// <param name="scale">The scaling factor.</param>
    /// <returns>The device-independent size.</returns>
    public static PixelSize FromSize(Size size, Vector2 scale)
    {
        return new PixelSize(
            (int)Math.Ceiling(size.Width * scale.X),
            (int)Math.Ceiling(size.Height * scale.Y));
    }

    /// <summary>
    /// Returns the string representation of the size.
    /// </summary>
    /// <returns>The string representation of the size.</returns>
    public override string ToString()
    {
        return FormattableString.Invariant($"{Width}, {Height}");
    }
}
