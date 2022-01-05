﻿using BEditorNext.Graphics;

namespace BEditorNext.Media;

/// <summary>
/// Paints an area with an <see cref="IDrawable"/>.
/// </summary>
public interface IDrawableBrush : ITileBrush
{
    /// <summary>
    /// Gets the drawable to draw.
    /// </summary>
    IDrawable? Drawable { get; }
}
