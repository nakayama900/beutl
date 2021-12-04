using System.Collections.Specialized;
using System.Numerics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Remote.Protocol.Input;
using Avalonia.VisualTree;

using BEditorNext.ProjectSystem;
using BEditorNext.ViewModels;

using FluentAvalonia.UI.Controls;

namespace BEditorNext.Views;

public partial class Timeline : UserControl
{
    private bool _seekbarIsMouseDown;
    private TimeSpan _clickedFrame;
    private int _clickedLayer;
    private TimeSpan _pointerFrame;
    private int _pointerLayer;
    private bool _isFirst;

    public Timeline()
    {
        InitializeComponent();

        ContentScroll.ScrollChanged += ContentScroll_ScrollChanged;
        ContentScroll.AddHandler(PointerWheelChangedEvent, ContentScroll_PointerWheelChanged, RoutingStrategies.Tunnel);
        ScaleScroll.AddHandler(PointerWheelChangedEvent, ContentScroll_PointerWheelChanged, RoutingStrategies.Tunnel);

        TimelinePanel.AddHandler(DragDrop.DragOverEvent, TimelinePanel_DragOver);
        TimelinePanel.AddHandler(DragDrop.DropEvent, TimelinePanel_Drop);
        DragDrop.SetAllowDrop(TimelinePanel, true);
    }

    private TimelineViewModel ViewModel => (TimelineViewModel)DataContext!;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        TimelinePanel.Children.RemoveRange(3, TimelinePanel.Children.Count - 3);

        ViewModel.Scene.Children.CollectionChanged += Children_CollectionChanged;
    }

    private void ContentScroll_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        Scene scene = ViewModel.Scene;
        if (_isFirst)
        {
            ContentScroll.Offset = new(scene.TimelineOptions.Offset.X, scene.TimelineOptions.Offset.Y);
            //_scrollLabel.Offset = new(0, Scene.TimeLineVerticalOffset);

            _isFirst = false;
        }

        scene.TimelineOptions = scene.TimelineOptions with
        {
            Offset = new Vector2((float)ContentScroll.Offset.X, (float)ContentScroll.Offset.Y)
        };

        ScaleScroll.Offset = new(ContentScroll.Offset.X, 0);
        //_scrollLabel.Offset = _scrollLabel.Offset.WithY(_scrollLine.Offset.Y);
    }

    private void ContentScroll_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        Scene scene = ViewModel.Scene;
        Avalonia.Vector offset = ContentScroll.Offset;

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            // 目盛りのスケールを変更
            float scale = scene.TimelineOptions.Scale;
            var ts = offset.X.ToTimeSpan(scale);
            float deltaScale = (float)(e.Delta.Y / 120) * 10 * scale;
            scene.TimelineOptions = scene.TimelineOptions with
            {
                Scale = deltaScale + scale,
            };

            offset = offset.WithX(ts.ToPixel(scene.TimelineOptions.Scale));
        }
        else if (e.KeyModifiers == KeyModifiers.Shift)
        {
            // オフセット(Y) をスクロール
            offset = offset.WithY(offset.Y - (e.Delta.Y * 50));
        }
        else
        {
            // オフセット(X) をスクロール
            offset = offset.WithX(offset.X - (e.Delta.Y * 50));
        }

        ContentScroll.Offset = offset;
        e.Handled = true;
    }

    // ポインター移動
    private void TimelinePanel_PointerMoved(object? sender, PointerEventArgs e)
    {
        PointerPoint pointerPt = e.GetCurrentPoint(TimelinePanel);
        _pointerFrame = pointerPt.Position.X.ToTimeSpan(ViewModel.Scene.TimelineOptions.Scale);
        _pointerLayer = pointerPt.Position.Y.ToLayerNumber();

        if (_seekbarIsMouseDown)
        {
            ViewModel.Scene.CurrentFrame = _pointerFrame;
        }
    }

    // ポインターが放された
    private void TimelinePanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        PointerPoint pointerPt = e.GetCurrentPoint(TimelinePanel);

        if (pointerPt.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
        {
            _seekbarIsMouseDown = false;

        }
    }

    // ポインターが押された
    private void TimelinePanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPoint pointerPt = e.GetCurrentPoint(TimelinePanel);
        _clickedFrame = pointerPt.Position.X.ToTimeSpan(ViewModel.Scene.TimelineOptions.Scale);
        _clickedLayer = pointerPt.Position.Y.ToLayerNumber();

        if (pointerPt.Properties.IsLeftButtonPressed)
        {
            _seekbarIsMouseDown = true;
            ViewModel.Scene.CurrentFrame = _clickedFrame;
        }
    }

    // ポインターが離れた
    private void TimelinePanel_PointerLeave(object? sender, PointerEventArgs e)
    {
        _seekbarIsMouseDown = false;
    }

    private void TimelinePanel_Drop(object? sender, DragEventArgs e)
    {
        TimelinePanel.Cursor = Cursors.Arrow;
        Scene scene = ViewModel.Scene;
        Point pt = e.GetPosition(TimelinePanel);

        _clickedFrame = pt.X.ToTimeSpan(scene.TimelineOptions.Scale);
        _clickedLayer = pt.Y.ToLayerNumber();

        if (e.Data.Get("RenderOperation") is RenderOperationRegistry.RegistryItem item)
        {
            ViewModel.AddLayer.Execute(new TimelineViewModel.LayerDescription(
                _clickedFrame, TimeSpan.FromSeconds(5), _clickedLayer, item));
        }
    }

    private void TimelinePanel_DragOver(object? sender, DragEventArgs e)
    {
        TimelinePanel.Cursor = Cursors.DragCopy;
        e.DragEffects = e.Data.Contains("RenderOperation") || (e.Data.GetFileNames()?.Any() ?? false) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void Children_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnLayersChanged(e);
    }

    private void OnLayersChanged(NotifyCollectionChangedEventArgs e)
    {

    }

    private void AddLayerClick(object? sender, RoutedEventArgs e)
    {

    }
}
