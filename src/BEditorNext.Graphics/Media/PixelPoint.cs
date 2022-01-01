﻿using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using System.Text.Json.Serialization;

using BEditorNext.Converters;
using BEditorNext.Graphics;
using BEditorNext.Utilities;

namespace BEditorNext.Media;

/// <summary>
/// Represents a point in device pixels.
/// </summary>
[JsonConverter(typeof(PixelPointJsonConverter))]
public readonly struct PixelPoint : IEquatable<PixelPoint>
{
    /// <summary>
    /// A point representing 0,0.
    /// </summary>
    public static readonly PixelPoint Origin = new(0, 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="PixelPoint"/> structure.
    /// </summary>
    /// <param name="x">The X co-ordinate.</param>
    /// <param name="y">The Y co-ordinate.</param>
    public PixelPoint(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Gets the X co-ordinate.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Gets the Y co-ordinate.
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Checks for equality between two <see cref="PixelPoint"/>s.
    /// </summary>
    /// <param name="left">The first point.</param>
    /// <param name="right">The second point.</param>
    /// <returns>True if the points are equal; otherwise false.</returns>
    public static bool operator ==(PixelPoint left, PixelPoint right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Checks for inequality between two <see cref="PixelPoint"/>s.
    /// </summary>
    /// <param name="left">The first point.</param>
    /// <param name="right">The second point.</param>
    /// <returns>True if the points are unequal; otherwise false.</returns>
    public static bool operator !=(PixelPoint left, PixelPoint right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Adds two points.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>A point that is the result of the addition.</returns>
    public static PixelPoint operator +(PixelPoint a, PixelPoint b)
    {
        return new PixelPoint(a.X + b.X, a.Y + b.Y);
    }

    /// <summary>
    /// Subtracts two points.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>A point that is the result of the subtraction.</returns>
    public static PixelPoint operator -(PixelPoint a, PixelPoint b)
    {
        return new PixelPoint(a.X - b.X, a.Y - b.Y);
    }

    /// <summary>
    /// Parses a <see cref="PixelPoint"/> string.
    /// </summary>
    /// <param name="s">The string.</param>
    /// <returns>The <see cref="PixelPoint"/>.</returns>
    public static PixelPoint Parse(string s)
    {
        using var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage: "Invalid PixelPoint.");
        return new PixelPoint(
            tokenizer.ReadInt32(),
            tokenizer.ReadInt32());
    }

    /// <summary>
    /// Returns a boolean indicating whether the point is equal to the other given point.
    /// </summary>
    /// <param name="other">The other point to test equality against.</param>
    /// <returns>True if this point is equal to other; False otherwise.</returns>
    public bool Equals(PixelPoint other)
    {
        return X == other.X && Y == other.Y;
    }

    /// <summary>
    /// Checks for equality between a point and an object.
    /// </summary>
    /// <param name="obj">The object.</param>
    /// <returns>
    /// True if <paramref name="obj"/> is a point that equals the current point.
    /// </returns>
    public override bool Equals(object? obj)
    {
        return obj is PixelPoint other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for a <see cref="PixelPoint"/>.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    /// <summary>
    /// Returns a new <see cref="PixelPoint"/> with the same Y co-ordinate and the specified X co-ordinate.
    /// </summary>
    /// <param name="x">The X co-ordinate.</param>
    /// <returns>The new <see cref="PixelPoint"/>.</returns>
    public PixelPoint WithX(int x)
    {
        return new PixelPoint(x, Y);
    }

    /// <summary>
    /// Returns a new <see cref="PixelPoint"/> with the same X co-ordinate and the specified Y co-ordinate.
    /// </summary>
    /// <param name="y">The Y co-ordinate.</param>
    /// <returns>The new <see cref="PixelPoint"/>.</returns>
    public PixelPoint WithY(int y)
    {
        return new PixelPoint(X, y);
    }

    /// <summary>
    /// Converts the <see cref="PixelPoint"/> to a device-independent <see cref="Point"/> using the
    /// specified scaling factor.
    /// </summary>
    /// <param name="scale">The scaling factor.</param>
    /// <returns>The device-independent point.</returns>
    public Point ToPoint(float scale)
    {
        return new Point(X / scale, Y / scale);
    }

    /// <summary>
    /// Converts the <see cref="PixelPoint"/> to a device-independent <see cref="Point"/> using the
    /// specified scaling factor.
    /// </summary>
    /// <param name="scale">The scaling factor.</param>
    /// <returns>The device-independent point.</returns>
    public Point ToPoint(Vector2 scale)
    {
        return new Point(X / scale.X, Y / scale.Y);
    }

    /// <summary>
    /// Converts a <see cref="Point"/> to device pixels using the specified scaling factor.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="scale">The scaling factor.</param>
    /// <returns>The device-independent point.</returns>
    public static PixelPoint FromPoint(Point point, float scale)
    {
        return new PixelPoint(
            (int)(point.X * scale),
            (int)(point.Y * scale));
    }

    /// <summary>
    /// Converts a <see cref="Point"/> to device pixels using the specified scaling factor.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="scale">The scaling factor.</param>
    /// <returns>The device-independent point.</returns>
    public static PixelPoint FromPoint(Point point, Vector2 scale)
    {
        return new PixelPoint(
            (int)(point.X * scale.X),
            (int)(point.Y * scale.Y));
    }

    /// <summary>
    /// Returns the string representation of the point.
    /// </summary>
    /// <returns>The string representation of the point.</returns>
    public override string ToString()
    {
        return FormattableString.Invariant($"{X}, {Y}");
    }
}
