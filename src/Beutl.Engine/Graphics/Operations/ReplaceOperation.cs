﻿using Beutl.Media;
using Beutl.Media.Pixel;

namespace Beutl.Graphics.Operations;

public readonly unsafe struct ReplaceOperation<TPixel>(Bitmap<TPixel> src, Bitmap<TPixel> dst, PixelRect roi)
    where TPixel : unmanaged, IPixel<TPixel>
{
    public readonly void Invoke(int y)
    {
        var sourceRow = src[y];
        var targetRow = dst[y + roi.Y].Slice(roi.X, roi.Width);

        sourceRow.CopyTo(targetRow);
    }
}
