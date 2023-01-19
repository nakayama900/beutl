﻿using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Collections.Pooled;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

using Beutl.Commands;
using Beutl.ProjectSystem;
using Beutl.Services.PrimitiveImpls;
using Beutl.ViewModels;
using Beutl.ViewModels.Tools;

using NuGet.Frameworks;

using static Beutl.Views.Timeline;

using Setter = Avalonia.Styling.Setter;

namespace Beutl.Views;

/*
 * 移動アニメーション中にUndoを行うと、
 * 表示される位置がUndo前になる。
 * 解決するには、オブジェクトがUndo/Redoしたかを追跡するAPIを追加して、
 * Undo/Redoがされた場合アニメーションをキャンセルする必要がある。
 */

public sealed partial class TimelineLayer : UserControl
{
    private Timeline? _timeline;
    private TimeSpan _pointerPosition;
    private IDisposable? _disposable1;

    public TimelineLayer()
    {
        InitializeComponent();

        textBox.LostFocus += OnTextBoxLostFocus;
    }

    public Func<TimeSpan> GetClickedTime => () => _pointerPosition;

    private TimelineLayerViewModel ViewModel => (TimelineLayerViewModel)DataContext!;

    protected override void OnAttachedToLogicalTree(Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        _timeline = this.FindLogicalAncestorOfType<Timeline>();

        BehaviorCollection behaviors = Interaction.GetBehaviors(this);
        behaviors.Clear();
        behaviors.Add(new _SelectBehavior());
        behaviors.Add(new _ResizeBehavior());
        behaviors.Add(new _MoveBehavior());
    }

    protected override void OnDetachedFromLogicalTree(Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        BehaviorCollection behaviors = Interaction.GetBehaviors(this);
        behaviors.Clear();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is TimelineLayerViewModel viewModel)
        {
            _disposable1?.Dispose();
            viewModel.AnimationRequested = async (args, token) =>
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    var animation1 = new Avalonia.Animation.Animation
                    {
                        Easing = new SplineEasing(0.1, 0.9, 0.2, 1.0),
                        Duration = TimeSpan.FromSeconds(0.25),
                        FillMode = FillMode.Forward,
                        Children =
                        {
                            new KeyFrame()
                            {
                                Cue = new Cue(0),
                                Setters =
                                {
                                    new Setter(MarginProperty, border.Margin),
                                    new Setter(WidthProperty, border.Width),
                                }
                            },
                            new KeyFrame()
                            {
                                Cue = new Cue(1),
                                Setters =
                                {
                                    new Setter(MarginProperty, args.BorderMargin),
                                    new Setter(WidthProperty, args.Width)
                                }
                            }
                        }
                    };
                    var animation2 = new Avalonia.Animation.Animation
                    {
                        Easing = new SplineEasing(0.1, 0.9, 0.2, 1.0),
                        Duration = TimeSpan.FromSeconds(0.25),
                        FillMode = FillMode.Forward,
                        Children =
                        {
                            new KeyFrame()
                            {
                                Cue = new Cue(0),
                                Setters =
                                {
                                    new Setter(MarginProperty, viewModel.Margin.Value)
                                }
                            },
                            new KeyFrame()
                            {
                                Cue = new Cue(1),
                                Setters =
                                {
                                    new Setter(MarginProperty, args.Margin)
                                }
                            }
                        }
                    };

                    Task task1 = animation1.RunAsync(border, null, token);
                    Task task2 = animation2.RunAsync(this, null, token);
                    await Task.WhenAll(task1, task2);
                });
            };
            _disposable1 = viewModel.Model.GetObservable(Layer.IsEnabledProperty)
                .Subscribe(b => Dispatcher.UIThread.InvokeAsync(() => border.Opacity = b ? 1 : 0.5));
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        Point point = e.GetPosition(this);
        float scale = ViewModel.Timeline.Options.Value.Scale;
        _pointerPosition = point.X.ToTimeSpan(scale);
    }

    private void AllowOutflowClick(object? sender, RoutedEventArgs e)
    {
        var layer = ViewModel.Model;
        var command = new ChangePropertyCommand<bool>(layer, Layer.AllowOutflowProperty, !layer.AllowOutflow, layer.AllowOutflow);
        command.DoAndRecord(CommandRecorder.Default);
    }

    private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        textBlock.IsVisible = true;
        textBox.IsVisible = false;
    }

    private void OpenNodeTree_Click(object? sender, RoutedEventArgs e)
    {
        Layer layer = ViewModel.Model;
        EditViewModel context = ViewModel.Timeline.EditorContext;
        NodeTreeTabViewModel? nodeTree = context.FindToolTab<NodeTreeTabViewModel>(
            v => v.Layer.Value == layer || v.Layer.Value == null);
        nodeTree ??= new NodeTreeTabViewModel(context);
        nodeTree.Layer.Value = layer;

        context.OpenToolTab(nodeTree);
    }

    private TimeSpan RoundStartTime(TimeSpan time, float scale, bool flag)
    {
        Layer layer = ViewModel.Model;

        if (!flag)
        {
            foreach (Layer item in ViewModel.Scene.Children.GetMarshal().Value)
            {
                if (item != layer)
                {
                    const double ThreadholdPixel = 10;
                    TimeSpan threadhold = ThreadholdPixel.ToTimeSpan(scale);
                    TimeSpan start = item.Start;
                    TimeSpan end = start + item.Length;
                    var startRange = new Media.TimeRange(start - threadhold, threadhold);
                    var endRange = new Media.TimeRange(end - threadhold, threadhold);

                    if (endRange.Contains(time))
                    {
                        return end;
                    }
                    else if (startRange.Contains(time))
                    {
                        return start;
                    }
                }
            }
        }

        return time;
    }

    private sealed class _ResizeBehavior : Behavior<TimelineLayer>
    {
        private Layer? _before;
        private Layer? _after;
        private bool _pressed;
        private AlignmentX _resizeType;

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.AddHandler(PointerMovedEvent, OnPointerMoved);
                AssociatedObject.border.AddHandler(PointerPressedEvent, OnBorderPointerPressed);
                AssociatedObject.border.AddHandler(PointerReleasedEvent, OnBorderPointerReleased);
                AssociatedObject.border.AddHandler(PointerMovedEvent, OnBorderPointerMoved);
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.RemoveHandler(PointerMovedEvent, OnPointerMoved);
                AssociatedObject.border.RemoveHandler(PointerPressedEvent, OnBorderPointerPressed);
                AssociatedObject.border.RemoveHandler(PointerMovedEvent, OnBorderPointerMoved);
            }
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (AssociatedObject is { ViewModel: { } viewModel } layer)
            {
                Point point = e.GetPosition(layer);
                float scale = viewModel.Timeline.Options.Value.Scale;
                TimeSpan pointerFrame = point.X.ToTimeSpan(scale);

                if (layer._timeline is { } timeline && _pressed)
                {
                    pointerFrame = layer.RoundStartTime(pointerFrame, scale, e.KeyModifiers.HasFlag(KeyModifiers.Alt));
                    point = point.WithX(pointerFrame.ToPixel(scale));

                    if (layer.Cursor != Cursors.Arrow && layer.Cursor is { })
                    {
                        double left = viewModel.BorderMargin.Value.Left;

                        if (_resizeType == AlignmentX.Right)
                        {
                            // 右
                            double x = _after == null ? point.X : Math.Min(_after.Start.ToPixel(scale), point.X);
                            viewModel.Width.Value = x - left;
                        }
                        else if (_resizeType == AlignmentX.Left && pointerFrame >= TimeSpan.Zero)
                        {
                            // 左
                            double x = _before == null ? point.X : Math.Max(_before.Range.End.ToPixel(scale), point.X);

                            viewModel.Width.Value += left - x;
                            viewModel.BorderMargin.Value = new Thickness(x, 0, 0, 0);
                        }

                        e.Handled = true;
                    }
                }
            }
        }

        private void OnBorderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (AssociatedObject is { _timeline: { }, border: { } border, ViewModel: { } viewModel } layer)
            {
                PointerPoint point = e.GetCurrentPoint(layer.border);
                if (point.Properties.IsLeftButtonPressed && e.KeyModifiers is KeyModifiers.None or KeyModifiers.Alt
                    && layer.Cursor != Cursors.Arrow && layer.Cursor is { })
                {
                    _before = viewModel.Model.GetBefore(viewModel.Model.ZIndex, viewModel.Model.Start);
                    _after = viewModel.Model.GetAfter(viewModel.Model.ZIndex, viewModel.Model.Range.End);
                    _pressed = true;

                    e.Handled = true;
                }
            }
        }

        private async void OnBorderPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_pressed)
            {
                _before = null;
                _after = null;
                _pressed = false;

                if (AssociatedObject is { ViewModel: { } viewModel })
                {
                    await viewModel.SyncModelToViewModel();
                    e.Handled = true;
                }
            }
        }

        private void OnBorderPointerMoved(object? sender, PointerEventArgs e)
        {
            if (AssociatedObject is { border: { } border } layer)
            {
                if (e.KeyModifiers is not (KeyModifiers.None or KeyModifiers.Alt))
                {
                    layer.Cursor = null;
                    _resizeType = AlignmentX.Center;
                }
                else if (!_pressed)
                {
                    Point point = e.GetPosition(border);
                    double horizon = point.X;

                    // 左右 10px内 なら左右矢印
                    if (horizon < 10)
                    {
                        layer.Cursor = Cursors.SizeWestEast;
                        _resizeType = AlignmentX.Left;
                    }
                    else if (horizon > border.Bounds.Width - 10)
                    {
                        layer.Cursor = Cursors.SizeWestEast;
                        _resizeType = AlignmentX.Right;
                    }
                    else
                    {
                        layer.Cursor = null;
                        _resizeType = AlignmentX.Center;
                    }
                }
            }
        }
    }

    private sealed class _MoveBehavior : Behavior<TimelineLayer>
    {
        private bool _pressed;
        private Point _start;

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.AddHandler(PointerMovedEvent, OnPointerMoved);
                AssociatedObject.border.AddHandler(PointerPressedEvent, OnBorderPointerPressed);
                AssociatedObject.border.AddHandler(PointerReleasedEvent, OnBorderPointerReleased);
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.RemoveHandler(PointerMovedEvent, OnPointerMoved);
                AssociatedObject.border.RemoveHandler(PointerPressedEvent, OnBorderPointerPressed);
                AssociatedObject.border.RemoveHandler(PointerReleasedEvent, OnBorderPointerReleased);
            }
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (AssociatedObject is { ViewModel: { } viewModel } layer
                && layer._timeline is { } timeline && _pressed)
            {
                Scene scene = viewModel.Scene;
                Point point = e.GetPosition(layer);
                float scale = viewModel.Timeline.Options.Value.Scale;
                TimeSpan pointerFrame = point.X.ToTimeSpan(scale);

                pointerFrame = layer.RoundStartTime(pointerFrame, scale, e.KeyModifiers.HasFlag(KeyModifiers.Alt));

                TimeSpan newframe = pointerFrame - _start.X.ToTimeSpan(scale);

                newframe = TimeSpan.FromTicks(Math.Clamp(newframe.Ticks, TimeSpan.Zero.Ticks, scene.Duration.Ticks));

                var newTop = Math.Max(e.GetPosition(timeline.TimelinePanel).Y - _start.Y, 0);
                var newLeft = newframe.ToPixel(scale);
                var deltaTop = newTop - viewModel.Margin.Value.Top;
                var deltaLeft = newLeft - viewModel.BorderMargin.Value.Left;

                viewModel.Margin.Value = new(0, newTop, 0, 0);
                viewModel.BorderMargin.Value = new Thickness(newLeft, 0, 0, 0);

                foreach (TimelineLayerViewModel item in viewModel.Timeline.GetSelected(viewModel))
                {
                    item.Margin.Value = new(0, item.Margin.Value.Top + deltaTop, 0, 0);
                    item.BorderMargin.Value = new(item.BorderMargin.Value.Left + deltaLeft, 0, 0, 0);
                }

                e.Handled = true;
            }
        }

        private void OnBorderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (AssociatedObject is { _timeline: { }, border: { } border } layer)
            {
                PointerPoint point = e.GetCurrentPoint(layer.border);
                if (point.Properties.IsLeftButtonPressed
                    && (layer.Cursor == Cursors.Arrow || layer.Cursor == null))
                {
                    _pressed = true;
                    _start = point.Position;
                    e.Handled = true;
                }
            }
        }

        private async void OnBorderPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_pressed)
            {
                _pressed = false;

                if (AssociatedObject is { ViewModel: { } viewModel })
                {
                    e.Handled = true;
                    var layers = new List<Layer>() { viewModel.Model };
                    layers.AddRange(viewModel.Timeline.GetSelected(viewModel).Select(x => x.Model));

                    if (layers.Count == 1)
                    {
                        await viewModel.SyncModelToViewModel();
                    }
                    else
                    {
                        float scale = viewModel.Timeline.Options.Value.Scale;
                        int rate = viewModel.Scene.Parent is Project proj ? proj.GetFrameRate() : 30;
                        TimeSpan newStart = viewModel.BorderMargin.Value.Left.ToTimeSpan(scale).RoundToRate(rate);
                        int newIndex = viewModel.Timeline.ToLayerNumber(viewModel.Margin.Value);
                        TimeSpan deltaStart = newStart - viewModel.Model.Start;
                        int deltaIndex = newIndex - viewModel.Model.ZIndex;

                        var animations = viewModel.Timeline.GetSelected(viewModel).Append(viewModel)
                            .Select(x => (ViewModel: x, Context: x.PrepareAnimation()))
                            .ToArray();

                        viewModel.Scene.MoveChildren(deltaIndex, deltaStart, layers.ToArray())
                            .DoAndRecord(CommandRecorder.Default);

                        foreach (var (item, context) in animations)
                        {
                            _ = item.AnimationRequest(context);
                        }
                    }
                }
            }
        }
    }

    private sealed class _SelectBehavior : Behavior<TimelineLayer>
    {
        private bool _pressedWithModifier;
        private Thickness _snapshot;
        private static readonly Avalonia.Animation.Animation s_animation1 = new()
        {
            Duration = TimeSpan.FromSeconds(0.083),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters =
                    {
                        new Setter(OpacityProperty, 1d),
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters =
                    {
                        new Setter(OpacityProperty, 0.8),
                    }
                }
            }
        };

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
                AssociatedObject.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
                AssociatedObject.border.AddHandler(PointerPressedEvent, OnBorderPointerPressed);
                AssociatedObject.border.AddHandler(PointerReleasedEvent, OnBorderPointerReleased);
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
                AssociatedObject.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
                AssociatedObject.border.RemoveHandler(PointerPressedEvent, OnBorderPointerPressed);
                AssociatedObject.border.RemoveHandler(PointerReleasedEvent, OnBorderPointerReleased);
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (AssociatedObject is { } obj)
            {
                obj.ZIndex = 5;
                if (!obj.textBox.IsFocused)
                {
                    obj.Focus();
                }
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (AssociatedObject is { } obj)
            {
                obj.ZIndex = 0;
            }
        }

        private async void OnBorderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (AssociatedObject is { _timeline: { } } obj)
            {
                PointerPoint point = e.GetCurrentPoint(obj.border);
                if (point.Properties.IsLeftButtonPressed)
                {
                    if (e.ClickCount == 2)
                    {
                        obj.textBlock.IsVisible = false;
                        obj.textBox.IsVisible = true;
                        obj.textBox.SelectAll();
                    }
                    else
                    {
                        s_animation1.PlaybackDirection = PlaybackDirection.Normal;
                        Task task1 = s_animation1.RunAsync(obj.border, null);

                        if (e.KeyModifiers is KeyModifiers.None or KeyModifiers.Alt)
                        {
                            EditViewModel editorContext = obj._timeline.ViewModel.EditorContext;
                            editorContext.SelectedObject.Value = obj.ViewModel.Model;
                            obj.ViewModel.IsSelected.Value = true;
                        }
                        else
                        {
                            Thickness margin = obj.ViewModel.Margin.Value;
                            Thickness borderMargin = obj.ViewModel.BorderMargin.Value;
                            _snapshot = new(borderMargin.Left, margin.Top, 0, 0);
                            _pressedWithModifier = true;
                        }

                        await task1;
                        obj.border.Opacity = 0.8;
                    }
                }
            }
        }

        private async void OnBorderPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (AssociatedObject is { _timeline: { } } obj)
            {
                if (_pressedWithModifier)
                {
                    Thickness margin = obj.ViewModel.Margin.Value;
                    Thickness borderMargin = obj.ViewModel.BorderMargin.Value;
                    if (borderMargin.Left == _snapshot.Left
                        && margin.Top == _snapshot.Top)
                    {
                        obj.ViewModel.IsSelected.Value = !obj.ViewModel.IsSelected.Value;
                    }

                    _pressedWithModifier = false;
                }

                s_animation1.PlaybackDirection = PlaybackDirection.Reverse;
                await s_animation1.RunAsync(obj.border, null);
                obj.border.Opacity = 1;
            }
        }
    }
}
