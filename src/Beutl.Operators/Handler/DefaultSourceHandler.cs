﻿using Beutl.Animation;
using Beutl.Operation;
using Beutl.ProjectSystem;
using Beutl.Rendering;

namespace Beutl.Operators.Handler;

public sealed class DefaultSourceHandler : SourceOperator, ISourceHandler
{
    private Layer? _layer;

    private static void Detach(Layer layer, IList<Renderable> renderables)
    {
        foreach (Renderable item in renderables)
        {
            if ((item as IHierarchical).HierarchicalParent is RenderLayerSpan span
                && layer.Span != span)
            {
                span.Value.Remove(item);
            }
        }
    }

    public void Handle(IList<Renderable> renderables, IClock clock)
    {
        if (_layer != null)
        {
            RenderLayerSpan span = _layer.Span;
            Detach(_layer, renderables);

            span.Value.Replace(renderables);

            foreach (Renderable item in span.Value.GetMarshal().Value)
            {
                item.ApplyStyling(clock);
                item.ApplyAnimations(clock);
                item.IsVisible = _layer.IsEnabled;
                while (!item.EndBatchUpdate())
                {
                }
            }

            renderables.Clear();
        }
    }

    public override void Exit()
    {
        base.Exit();

        if (_layer != null)
        {
            RenderLayerSpan span = _layer.Span;
            span.Value.Clear();
        }
    }

    protected override void OnAttachedToHierarchy(in HierarchyAttachmentEventArgs args)
    {
        base.OnAttachedToHierarchy(args);
        _layer = args.Parent as Layer;
    }

    protected override void OnDetachedFromHierarchy(in HierarchyAttachmentEventArgs args)
    {
        base.OnDetachedFromHierarchy(args);

        if (_layer != null)
        {
            RenderLayerSpan span = _layer.Span;
            span.Value.Clear();
            _layer = null;
        }
    }
}
