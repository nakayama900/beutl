﻿using BEditorNext.Media;
using BEditorNext.Media.Pixel;

namespace BEditorNext.Graphics.Effects;

public enum EffectType
{
    Bitmap,

    Pixel,

    Row
}

public abstract unsafe class BitmapEffect
{
    private readonly struct PixelEffectOp
    {
        private readonly Bgra8888* _ptr;
        private readonly IReadOnlyList<BitmapEffect> _effects;
        private readonly int _start;
        private readonly int _end;
        private readonly BitmapInfo _info;

        public PixelEffectOp(Bgra8888* ptr, IReadOnlyList<BitmapEffect> effects, int start, int length, BitmapInfo info)
        {
            _ptr = ptr;
            _effects = effects;
            _start = start;
            _end = length + start;
            _info = info;
        }

        public void Invoke(int pos)
        {
            for (int i = _start; i < _end; i++)
            {
                if (_effects[i] is PixelEffect pe)
                {
                    pe.Apply(ref _ptr[pos], _info, pos);
                }
            }
        }
    }

    private readonly struct RowEffectOp
    {
        private readonly Bgra8888* _ptr;
        private readonly IReadOnlyList<BitmapEffect> _effects;
        private readonly int _start;
        private readonly int _end;
        private readonly BitmapInfo _info;

        public RowEffectOp(Bgra8888* ptr, IReadOnlyList<BitmapEffect> effects, int start, int length, BitmapInfo info)
        {
            _ptr = ptr;
            _effects = effects;
            _start = start;
            _end = length + start;
            _info = info;
        }

        public void Invoke(int pos)
        {
            var span = new Span<Bgra8888>(_ptr, pos * _info.Width);

            for (int i = _start; i < _end; i++)
            {
                if (_effects[i] is RowEffect re)
                {
                    re.Apply(span, _info, pos);
                }
            }
        }
    }

    private static void ApplyPixelEffect(Bitmap<Bgra8888> bitmap, IReadOnlyList<BitmapEffect> effects, int start, int length)
    {
        Parallel.For(0, bitmap.Width * bitmap.Height,
            new PixelEffectOp((Bgra8888*)bitmap.Data, effects, start, length, bitmap.Info).Invoke);
    }

    private static void ApplyRowEffect(Bitmap<Bgra8888> bitmap, IReadOnlyList<BitmapEffect> effects, int start, int length)
    {
        Parallel.For(0, bitmap.Height,
            new RowEffectOp((Bgra8888*)bitmap.Data, effects, start, length, bitmap.Info).Invoke);
    }

    private static void ApplyBitmapEffect(ref Bitmap<Bgra8888> bitmap, IReadOnlyList<BitmapEffect> effects, int start, int length)
    {
        int l = start + length;
        for (int i = start; i < l; i++)
        {
            effects[i].Apply(ref bitmap);
        }
    }

    public static Bitmap<Bgra8888> ApplyAll(Bitmap<Bgra8888> bitmap, IReadOnlyList<BitmapEffect> effects)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            BitmapEffect effect = effects[i];
            int start = i;
            int length = 0;

            if (effect.EffectType == EffectType.Pixel)
            {
                for (; i < effects.Count; i++)
                {
                    BitmapEffect effect2 = effects[i];

                    if (effect2.EffectType == EffectType.Pixel)
                    {
                        length++;
                    }
                    else
                    {
                        i--;
                        break;
                    }
                }

                ApplyPixelEffect(bitmap, effects, start, length);
            }
            else if (effect.EffectType == EffectType.Row)
            {
                for (; i < effects.Count; i++)
                {
                    BitmapEffect effect2 = effects[i];

                    if (effect2.EffectType == EffectType.Row)
                    {
                        length++;
                    }
                    else
                    {
                        i--;
                        break;
                    }
                }

                ApplyRowEffect(bitmap, effects, start, length);
            }
            else
            {
                for (; i < effects.Count; i++)
                {
                    BitmapEffect effect2 = effects[i];

                    if (effect2.EffectType == EffectType.Bitmap)
                    {
                        length++;
                    }
                    else
                    {
                        i--;
                        break;
                    }
                }

                ApplyBitmapEffect(ref bitmap, effects, start, length);
            }
        }

        return bitmap;
    }

    protected EffectType EffectType { get; set; } = EffectType.Bitmap;

    public virtual PixelSize Measure(PixelSize size)
    {
        return size;
    }

    public abstract void Apply(ref Bitmap<Bgra8888> bitmap);
}

public abstract unsafe class PixelEffect : BitmapEffect
{
    protected PixelEffect()
    {
        EffectType = EffectType.Pixel;
    }

    public abstract void Apply(ref Bgra8888 pixel, in BitmapInfo info, int index);

    public override void Apply(ref Bitmap<Bgra8888> bitmap)
    {
        Bitmap<Bgra8888>? b = bitmap;

        Parallel.For(0, bitmap.Width * bitmap.Height, pos =>
        {
            var ptr = (Bgra8888*)b.Data;
            Apply(ref ptr[pos], b.Info, pos);
        });
    }
}

public abstract unsafe class RowEffect : BitmapEffect
{
    protected RowEffect()
    {
        EffectType = EffectType.Row;
    }

    public abstract void Apply(Span<Bgra8888> pixel, in BitmapInfo info, int row);

    public override void Apply(ref Bitmap<Bgra8888> bitmap)
    {
        Bitmap<Bgra8888>? b = bitmap;

        Parallel.For(0, bitmap.Height, pos =>
        {
            Span<Bgra8888> span = b.DataSpan[(pos * b.Width)..];
            Apply(span, b.Info, pos);
        });
    }
}
