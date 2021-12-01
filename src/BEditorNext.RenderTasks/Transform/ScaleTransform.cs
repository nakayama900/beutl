﻿using System.Numerics;
using BEditorNext.ProjectItems;

namespace BEditorNext.RenderTasks.Transform;

public sealed class ScaleTransform : RenderTask
{
    public static readonly PropertyDefine<float> ScaleProperty;
    public static readonly PropertyDefine<float> ScaleXProperty;
    public static readonly PropertyDefine<float> ScaleYProperty;

    static ScaleTransform()
    {
        ScaleProperty = RegisterProperty<float, ScaleTransform>(nameof(Scale), (owner, obj) => owner.Scale = obj, owner => owner.Scale)
            .EnableEditor()
            .EnableAnimation()
            .DefaultValue(100f)
            .JsonName("scale");

        ScaleXProperty = RegisterProperty<float, ScaleTransform>(nameof(ScaleX), (owner, obj) => owner.ScaleX = obj, owner => owner.ScaleX)
            .EnableEditor()
            .EnableAnimation()
            .DefaultValue(100f)
            .JsonName("scaleX");

        ScaleYProperty = RegisterProperty<float, ScaleTransform>(nameof(ScaleY), (owner, obj) => owner.ScaleY = obj, owner => owner.ScaleY)
            .EnableEditor()
            .EnableAnimation()
            .DefaultValue(100f)
            .JsonName("scaleY");
    }

    public float Scale { get; set; }

    public float ScaleX { get; set; }

    public float ScaleY { get; set; }

    public override void Execute(in RenderTaskExecuteArgs args)
    {
        args.Renderer.Graphics.Scale(new Vector2(ScaleX, ScaleY) * Scale / 10000);
    }
}
