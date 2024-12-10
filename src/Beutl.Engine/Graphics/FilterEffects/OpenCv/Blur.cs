﻿using System.ComponentModel.DataAnnotations;

using Beutl.Language;
using Beutl.Media;
using Beutl.Media.Pixel;

using OpenCvSharp;

using SkiaSharp;

namespace Beutl.Graphics.Effects.OpenCv;

public class Blur : FilterEffect
{
    public static readonly CoreProperty<PixelSize> KernelSizeProperty;
    public static readonly CoreProperty<bool> FixImageSizeProperty;
    private PixelSize _kernelSize;
    private bool _fixImageSize;

    static Blur()
    {
        KernelSizeProperty = ConfigureProperty<PixelSize, Blur>(nameof(KernelSize))
            .Accessor(o => o.KernelSize, (o, v) => o.KernelSize = v)
            .DefaultValue(PixelSize.Empty)
            .Register();

        FixImageSizeProperty = ConfigureProperty<bool, Blur>(nameof(FixImageSize))
            .Accessor(o => o.FixImageSize, (o, v) => o.FixImageSize = v)
            .DefaultValue(false)
            .Register();

        AffectsRender<Blur>(KernelSizeProperty, FixImageSizeProperty);
    }

    [Display(Name = nameof(Strings.KernelSize), ResourceType = typeof(Strings))]
    [Range(typeof(PixelSize), "0,0", "max,max")]
    public PixelSize KernelSize
    {
        get => _kernelSize;
        set => SetAndRaise(KernelSizeProperty, ref _kernelSize, value);
    }

    [Display(Name = nameof(Strings.FixImageSize), ResourceType = typeof(Strings))]
    public bool FixImageSize
    {
        get => _fixImageSize;
        set => SetAndRaise(FixImageSizeProperty, ref _fixImageSize, value);
    }

    public override void ApplyTo(FilterEffectContext context)
    {
        context.CustomEffect((KernelSize, FixImageSize), Apply, TransformBounds);
    }

    private static Rect TransformBounds((PixelSize KernelSize, bool FixImageSize) data, Rect rect)
    {
        if (!data.FixImageSize)
        {
            int kwidth = data.KernelSize.Width;
            int kheight = data.KernelSize.Height;

            if (kwidth % 2 == 0)
                kwidth++;
            if (kheight % 2 == 0)
                kheight++;

            int halfWidth = kwidth / 2;
            int halfHeight = kheight / 2;
            rect = rect.Inflate(new Thickness(halfWidth, halfHeight));
        }
        return rect;
    }

    private static void Apply((PixelSize KernelSize, bool FixImageSize) data, CustomFilterEffectContext context)
    {
        for (int i = 0; i < context.Targets.Count; i++)
        {
            var target = context.Targets[i];
            var renderTarget = target.RenderTarget!;

            int kWidth = data.KernelSize.Width;
            int kHeight = data.KernelSize.Height;
            if (kWidth <= 0 || kHeight <= 0)
                return;

            if (kWidth % 2 == 0)
                kWidth++;
            if (kHeight % 2 == 0)
                kHeight++;

            Bitmap<Bgra8888>? dst = null;

            try
            {
                using (var src = renderTarget.Snapshot())
                {
                    if (data.FixImageSize)
                    {
                        dst = src.Clone();
                    }
                    else
                    {
                        dst = src.MakeBorder(src.Width + kWidth, src.Height + kHeight);
                    }
                }

                using var mat = dst.ToMat();
                Cv2.Blur(mat, mat, new(kWidth, kHeight));

                EffectTarget newTarget = context.CreateTarget(TransformBounds(data, target.Bounds));
                newTarget.RenderTarget!.Value.Canvas.DrawBitmap(dst.ToSKBitmap(), 0, 0);
                target.Dispose();
                context.Targets[i] = newTarget;
            }
            finally
            {
                dst?.Dispose();
            }
        }
    }

    public override Rect TransformBounds(Rect rect)
    {
        return TransformBounds((_kernelSize, _fixImageSize), rect);
    }
}
