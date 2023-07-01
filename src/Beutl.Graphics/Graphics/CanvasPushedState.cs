﻿using Beutl.Graphics.Filters;

using SkiaSharp;

namespace Beutl.Graphics;

public partial class Canvas
{
    private abstract record CanvasPushedState
    {
        internal record SKCanvasPushedState(int Count) : CanvasPushedState
        {
            public override void Pop(Canvas canvas)
            {
                canvas._canvas.RestoreToCount(Count);
                canvas._currentTransform = canvas._canvas.TotalMatrix.ToMatrix();
            }
        }

        internal record MaskPushedState(int Count, bool Invert, SKPaint Paint) : CanvasPushedState
        {
            public override void Pop(Canvas canvas)
            {
                canvas._sharedFillPaint.Reset();
                canvas._sharedFillPaint.BlendMode = Invert ? SKBlendMode.DstOut : SKBlendMode.DstIn;

                canvas._canvas.SaveLayer(canvas._sharedFillPaint);
                using (SKPaint maskPaint = Paint)
                {
                    canvas._canvas.DrawPaint(maskPaint);
                }

                canvas._canvas.Restore();

                canvas._canvas.RestoreToCount(Count);
            }
        }

        internal record ImageFilterPushedState(int Count, IImageFilter ImageFilter, SKPaint Paint) : CanvasPushedState
        {
            public override void Pop(Canvas canvas)
            {
                canvas._canvas.RestoreToCount(Count);
                Paint.Dispose();
            }
        }

        internal record BlendModePushedState(BlendMode BlendMode) : CanvasPushedState
        {
            public override void Pop(Canvas canvas)
            {
                canvas.BlendMode = BlendMode;
            }
        }

        public abstract void Pop(Canvas canvas);
    }
}
