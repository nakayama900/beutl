﻿using Beutl.Media;
using Beutl.Media.Pixel;

namespace Beutl.Graphics.Operations;

public readonly unsafe struct CropOperation<TPixel>(Bitmap<TPixel> src, Bitmap<TPixel> dst, PixelRect roi)
    where TPixel : unmanaged, IPixel<TPixel>
{
    public readonly void Invoke(int y)
    {
        var sourceRow = src[y + roi.Y].Slice(roi.X, roi.Width);
        var targetRow = dst[y];

        sourceRow.Slice(0, roi.Width).CopyTo(targetRow);
    }
}
