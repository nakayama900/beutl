﻿using BeUtl.Animation;
using BeUtl.Graphics.Effects;
using BeUtl.Graphics.Filters;
using BeUtl.Graphics.Transformation;
using BeUtl.Media;
using BeUtl.Media.Pixel;
using BeUtl.Rendering;

namespace BeUtl.Graphics;

public abstract class Drawable : Renderable, IDrawable, ILogicalElement
{
    public static readonly CoreProperty<float> WidthProperty;
    public static readonly CoreProperty<float> HeightProperty;
    public static readonly CoreProperty<ITransform?> TransformProperty;
    public static readonly CoreProperty<IImageFilter?> FilterProperty;
    public static readonly CoreProperty<IBitmapEffect?> EffectProperty;
    public static readonly CoreProperty<AlignmentX> AlignmentXProperty;
    public static readonly CoreProperty<AlignmentY> AlignmentYProperty;
    public static readonly CoreProperty<RelativePoint> TransformOriginProperty;
    public static readonly CoreProperty<IBrush?> ForegroundProperty;
    public static readonly CoreProperty<IBrush?> OpacityMaskProperty;
    public static readonly CoreProperty<BlendMode> BlendModeProperty;
    private float _width = 0;
    private float _height = 0;
    private ITransform? _transform;
    private IImageFilter? _filter;
    private IBitmapEffect? _effect;
    private AlignmentX _alignX;
    private AlignmentY _alignY;
    private RelativePoint _transformOrigin;
    private IBrush? _foreground;
    private IBrush? _opacityMask;
    private BlendMode _blendMode = BlendMode.SrcOver;

    static Drawable()
    {
        WidthProperty = ConfigureProperty<float, Drawable>(nameof(Width))
            .Accessor(o => o.Width, (o, v) => o.Width = v)
            .PropertyFlags(PropertyFlags.All)
            .DefaultValue(0)
            .Minimum(0)
            .SerializeName("width")
            .Register();

        HeightProperty = ConfigureProperty<float, Drawable>(nameof(Height))
            .Accessor(o => o.Height, (o, v) => o.Height = v)
            .PropertyFlags(PropertyFlags.All)
            .DefaultValue(0)
            .Minimum(0)
            .SerializeName("height")
            .Register();

        TransformProperty = ConfigureProperty<ITransform?, Drawable>(nameof(Transform))
            .Accessor(o => o.Transform, (o, v) => o.Transform = v)
            .PropertyFlags(PropertyFlags.All & ~PropertyFlags.Animatable)
            .DefaultValue(null)
            .SerializeName("transform")
            .Register();

        FilterProperty = ConfigureProperty<IImageFilter?, Drawable>(nameof(Filter))
            .Accessor(o => o.Filter, (o, v) => o.Filter = v)
            .PropertyFlags(PropertyFlags.All & ~PropertyFlags.Animatable)
            .DefaultValue(null)
            .SerializeName("filter")
            .Register();

        EffectProperty = ConfigureProperty<IBitmapEffect?, Drawable>(nameof(Effect))
            .Accessor(o => o.Effect, (o, v) => o.Effect = v)
            .PropertyFlags(PropertyFlags.All & ~PropertyFlags.Animatable)
            .DefaultValue(null)
            .SerializeName("effect")
            .Register();

        AlignmentXProperty = ConfigureProperty<AlignmentX, Drawable>(nameof(AlignmentX))
            .Accessor(o => o.AlignmentX, (o, v) => o.AlignmentX = v)
            .PropertyFlags(PropertyFlags.All)
            .DefaultValue(AlignmentX.Left)
            .SerializeName("align-x")
            .Register();

        AlignmentYProperty = ConfigureProperty<AlignmentY, Drawable>(nameof(AlignmentY))
            .Accessor(o => o.AlignmentY, (o, v) => o.AlignmentY = v)
            .PropertyFlags(PropertyFlags.All)
            .DefaultValue(AlignmentY.Top)
            .SerializeName("align-y")
            .Register();

        TransformOriginProperty = ConfigureProperty<RelativePoint, Drawable>(nameof(TransformOrigin))
            .Accessor(o => o.TransformOrigin, (o, v) => o.TransformOrigin = v)
            .PropertyFlags(PropertyFlags.All)
            .SerializeName("transform-origin")
            .Register();

        ForegroundProperty = ConfigureProperty<IBrush?, Drawable>(nameof(Foreground))
            .Accessor(o => o.Foreground, (o, v) => o.Foreground = v)
            .PropertyFlags(PropertyFlags.All & ~PropertyFlags.Animatable)
            .SerializeName("foreground")
            .Register();

        OpacityMaskProperty = ConfigureProperty<IBrush?, Drawable>(nameof(OpacityMask))
            .Accessor(o => o.OpacityMask, (o, v) => o.OpacityMask = v)
            .PropertyFlags(PropertyFlags.All & ~PropertyFlags.Animatable)
            .DefaultValue(null)
            .SerializeName("opacity-mask")
            .Register();

        BlendModeProperty = ConfigureProperty<BlendMode, Drawable>(nameof(BlendMode))
            .Accessor(o => o.BlendMode, (o, v) => o.BlendMode = v)
            .PropertyFlags(PropertyFlags.All)
            .DefaultValue(BlendMode.SrcOver)
            .SerializeName("blend-mode")
            .Register();

        AffectsRender<Drawable>(
            WidthProperty, HeightProperty,
            TransformProperty, FilterProperty, EffectProperty,
            AlignmentXProperty, AlignmentYProperty,
            TransformOriginProperty,
            ForegroundProperty, OpacityMaskProperty,
            BlendModeProperty);
    }

    public float Width
    {
        get => _width;
        set => SetAndRaise(WidthProperty, ref _width, value);
    }

    public float Height
    {
        get => _height;
        set => SetAndRaise(HeightProperty, ref _height, value);
    }

    public Rect Bounds { get; private set; }

    public ITransform? Transform
    {
        get => _transform;
        set => SetAndRaise(TransformProperty, ref _transform, value);
    }

    public IImageFilter? Filter
    {
        get => _filter;
        set => SetAndRaise(FilterProperty, ref _filter, value);
    }

    public IBitmapEffect? Effect
    {
        get => _effect;
        set => SetAndRaise(EffectProperty, ref _effect, value);
    }

    public AlignmentX AlignmentX
    {
        get => _alignX;
        set => SetAndRaise(AlignmentXProperty, ref _alignX, value);
    }

    public AlignmentY AlignmentY
    {
        get => _alignY;
        set => SetAndRaise(AlignmentYProperty, ref _alignY, value);
    }

    public RelativePoint TransformOrigin
    {
        get => _transformOrigin;
        set => SetAndRaise(TransformOriginProperty, ref _transformOrigin, value);
    }

    public IBrush? Foreground
    {
        get => _foreground;
        set => SetAndRaise(ForegroundProperty, ref _foreground, value);
    }

    public IBrush? OpacityMask
    {
        get => _opacityMask;
        set => SetAndRaise(OpacityMaskProperty, ref _opacityMask, value);
    }

    public BlendMode BlendMode
    {
        get => _blendMode;
        set => SetAndRaise(BlendModeProperty, ref _blendMode, value);
    }

    public IBitmap ToBitmap()
    {
        Size size = MeasureCore(Size.Infinity);
        using (var canvas = new Canvas((int)size.Width, (int)size.Height))
        using (Foreground == null ? new() : canvas.PushForeground(Foreground))
        {
            OnDraw(canvas);
            return canvas.GetBitmap();
        }
    }

    public void Measure(Size availableSize)
    {
        Size size = MeasureCore(availableSize);
        Matrix transform = GetTransformMatrix(availableSize, size);
        var rect = new Rect(size);

        if (_effect != null)
        {
            rect = _effect.TransformBounds(rect);
        }

        if (_filter != null)
        {
            rect = _filter.TransformBounds(rect);
        }

        Bounds = rect.TransformToAABB(transform);
    }

    protected abstract Size MeasureCore(Size availableSize);

    private Matrix GetTransformMatrix(Size availableSize, Size coreBounds)
    {
        Vector pt = CreatePoint(coreBounds);
        Vector origin = CreateRelPoint(availableSize);
        Matrix offset = Matrix.CreateTranslation(origin);

        if (Transform is { })
        {
            return Matrix.CreateTranslation(pt) * Transform.Value * offset;
        }
        else
        {
            return offset * Matrix.CreateTranslation(pt);
        }
    }

    private void HasBitmapEffect(ICanvas canvas)
    {
        Size availableSize = canvas.Size.ToSize(1);
        Size size = MeasureCore(availableSize);
        var rect = new Rect(size);
        Matrix transform = GetTransformMatrix(availableSize, size);
        Matrix transformFact = transform;
        if (_effect != null)
        {
            rect = _effect.TransformBounds(rect);
            transformFact = transform.Append(Matrix.CreateTranslation(rect.Position));
        }
        if (_filter != null)
        {
            rect = _filter.TransformBounds(rect);
        }

        rect = rect.TransformToAABB(transform);
        using (Foreground == null ? new() : canvas.PushForeground(Foreground))
        using (canvas.PushBlendMode(BlendMode))
        using (canvas.PushTransform(transformFact))
        using (_filter == null ? new() : canvas.PushFilters(_filter))
        using (OpacityMask == null ? new() : canvas.PushOpacityMask(OpacityMask, rect))
        {
            IBitmap bitmap = ToBitmap();
            if (bitmap is Bitmap<Bgra8888> bmp)
            {
                _effect!.Processor.Process(in bmp, out Bitmap<Bgra8888> outBmp);
                if (bmp != outBmp)
                {
                    bmp.Dispose();
                }

                //if (Width > 0 && Height > 0)
                //{
                //    using (canvas.PushTransform(Matrix.CreateScale(Width / outBmp.Width, Height / outBmp.Height)))
                //    {
                //        canvas.DrawBitmap(outBmp);
                //    }
                //}
                //else
                {
                    canvas.DrawBitmap(outBmp);
                }

                outBmp.Dispose();
            }
            else
            {
                bitmap.Dispose();
                OnDraw(canvas);
            }
        }

        Bounds = rect;
    }

    public void Draw(ICanvas canvas)
    {
        if (IsVisible)
        {
            if (_effect != null)
            {
                HasBitmapEffect(canvas);
            }
            else
            {
                Size availableSize = canvas.Size.ToSize(1);
                Size size = MeasureCore(availableSize);
                var rect = new Rect(size);
                if (_filter != null)
                {
                    rect = _filter.TransformBounds(rect);
                }

                Matrix transform = GetTransformMatrix(availableSize, size);

                using (Foreground == null ? new() : canvas.PushForeground(Foreground))
                using (canvas.PushBlendMode(BlendMode))
                using (canvas.PushTransform(transform))
                using (_filter == null ? new() : canvas.PushFilters(_filter))
                using (OpacityMask == null ? new() : canvas.PushOpacityMask(OpacityMask, new Rect(size)))
                {
                    OnDraw(canvas);
                }

                Bounds = rect.TransformToAABB(transform);
            }
        }

        IsDirty = false;
#if DEBUG
        //Rect bounds = Bounds.Inflate(10);
        //using (canvas.PushTransform(Matrix.CreateTranslation(bounds.Position)))
        //using (canvas.PushStrokeWidth(5))
        //{
        //    canvas.DrawRect(bounds.Size);
        //}
#endif
    }

    public override void Render(IRenderer renderer)
    {
        Draw(renderer.Graphics);
    }

    public override void ApplyAnimations(IClock clock)
    {
        base.ApplyAnimations(clock);
        (Transform as Animatable)?.ApplyAnimations(clock);
        (Filter as Animatable)?.ApplyAnimations(clock);
        (Effect as Animatable)?.ApplyAnimations(clock);
        (Foreground as Animatable)?.ApplyAnimations(clock);
        (OpacityMask as Animatable)?.ApplyAnimations(clock);
    }

    protected abstract void OnDraw(ICanvas canvas);

    private Point CreateRelPoint(Size size)
    {
        return TransformOrigin.ToPixels(size);
    }

    private Point CreatePoint(Size canvasSize)
    {
        float x = 0;
        float y = 0;

        if (AlignmentX == AlignmentX.Center)
        {
            x -= canvasSize.Width / 2;
        }
        else if (AlignmentX == AlignmentX.Right)
        {
            x -= canvasSize.Width;
        }

        if (AlignmentY == AlignmentY.Center)
        {
            y -= canvasSize.Height / 2;
        }
        else if (AlignmentY == AlignmentY.Bottom)
        {
            y -= canvasSize.Height;
        }

        return new Point(x, y);
    }
}
