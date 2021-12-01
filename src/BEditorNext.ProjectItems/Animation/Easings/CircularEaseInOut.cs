﻿namespace BEditorNext.Animation.Easings;

public sealed class CircularEaseInOut : Easing
{
    public override float Ease(float progress)
    {
        return Funcs.CircularEaseInOut(progress);
    }
}
