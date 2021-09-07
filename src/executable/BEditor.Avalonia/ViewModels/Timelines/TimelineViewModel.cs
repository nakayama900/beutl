﻿using System;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Input;

using BEditor.Data;
using BEditor.Extensions;
using BEditor.Media;
using BEditor.Models;
using BEditor.Properties;

using Microsoft.Extensions.DependencyInjection;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace BEditor.ViewModels.Timelines
{
    public sealed class TimelineViewModel
    {
        public int ClickedLayer;
        public Frame ClickedFrame;
        public int PointerLayer;
        public Frame PointerFrame;
        public bool SeekbarIsMouseDown;
        public bool ClipMouseDown;
        public bool KeyframeToggle = true;
        public byte ClipLeftRight;
        public ClipElement? SelectedClip;
        public Point ClipStartAbs;
        public Point ClipStartRel;
        public bool ClipTimeChange;

        public TimelineViewModel(Scene scene)
        {
            Scene = scene;

            TrackWidth.Value = scene.ToPixel(scene.TotalFrame);

            scene.ObserveProperty(s => s.PreviewFrame)
                .Where(_ => Extensions.Tool.PreviewIsEnabled)
                .Subscribe(async f =>
                {
                    SeekbarMargin.Value = new Thickness(Scene.ToPixel(f), 0, 0, 0);

                    var type = AppModel.Current.AppStatus is Status.Playing ? ApplyType.Video : ApplyType.Edit;

                    await Scene.Parent.PreviewUpdateAsync(type);
                });

            scene.ObserveProperty(s => s.TotalFrame)
                .Subscribe(_ => TrackWidth.Value = Scene.ToPixel(Scene.TotalFrame));

            AddClip.Subscribe(meta =>
            {
                if (!Scene.InRange(ClickedFrame, ClickedFrame + 180, ClickedLayer))
                {
                    Scene.ServiceProvider?.GetService<IMessage>()?.Snackbar(Strings.ClipExistsInTheSpecifiedLocation, string.Empty);

                    return;
                }

                Scene.AddClip(ClickedFrame, ClickedLayer, meta, out _).Execute();
            });
        }

        public Func<PointerEventArgs, Point>? GetLayerMousePosition { get; set; }

        public double TrackHeight { get; } = ConstantSettings.ClipHeight;

        public ReactiveCommand<ObjectMetadata> AddClip { get; } = new();

        public ReactiveCommand Paste { get; } = new();

        public ReactiveProperty<StandardCursorType> LayerCursor { get; } = new();

        public ReactiveProperty<double> TrackWidth { get; } = new();

        public ReactivePropertySlim<Thickness> SeekbarMargin { get; } = new();

        public Scene Scene { get; }

        public Action<ClipElement, int>? ClipLayerMoveCommand { get; set; }

        public static int ToLayer(double pixel)
        {
            return (int)(pixel / ConstantSettings.ClipHeight) + 1;
        }

        public static double ToLayerPixel(int layer)
        {
            return (layer - 1) * ConstantSettings.ClipHeight;
        }

        public void ScalePointerMoved(Point point)
        {
            PointerFrame = Scene.ToFrame(point.X);

            if (SeekbarIsMouseDown && KeyframeToggle)
            {
                Scene.PreviewFrame = PointerFrame;
            }
        }

        public void PointerMoved(Point point)
        {
            // point: マウスの現在フレーム

            PointerFrame = Scene.ToFrame(point.X);
            PointerLayer = ToLayer(point.Y);

            if (SeekbarIsMouseDown && KeyframeToggle)
            {
                Scene.PreviewFrame = PointerFrame;
            }
            else if (ClipMouseDown)
            {
                if (SelectedClip is null) return;
                var selectviewmodel = SelectedClip.GetCreateClipViewModel();

                if (selectviewmodel.ClipCursor.Value == StandardCursorType.Arrow && LayerCursor.Value == StandardCursorType.Arrow)
                {
                    var newframe = PointerFrame - Scene.ToFrame(ClipStartRel.X);
                    var newlayer = PointerLayer;

                    if (!Scene.InRange(SelectedClip, newframe, newframe + SelectedClip.Length, newlayer))
                    {
                        return;
                    }

                    newlayer = Math.Clamp(newlayer, 1, 100);
                    newframe = Math.Clamp(newframe, 0, Scene.TotalFrame);
                    var thickness = new Thickness(Scene.ToPixel(newframe), ToLayerPixel(newlayer), 0, 0);

                    selectviewmodel.Row = newlayer;
                    selectviewmodel.MarginProperty.Value = thickness;

                    ClipTimeChange = true;
                }
                else
                {
                    var move = Scene.ToPixel(PointerFrame - Scene.ToFrame(ClipStartAbs.X)); //一時的な移動量
                    if (ClipLeftRight == 2)
                    {
                        if (!Scene.InRange(
                            SelectedClip,
                            Scene.ToFrame(selectviewmodel.MarginLeft),
                            Scene.ToFrame(selectviewmodel.WidthProperty.Value + move + selectviewmodel.MarginLeft),
                            SelectedClip.Layer,
                            out var near))
                        {
                            selectviewmodel.WidthProperty.Value = Scene.ToPixel(near.Start - SelectedClip.Start);

                            return;
                        }

                        // 左
                        selectviewmodel.WidthProperty.Value += move;
                    }
                    else if (ClipLeftRight == 1 && PointerFrame > 0)
                    {
                        if (!Scene.InRange(
                            SelectedClip,
                            Scene.ToFrame(selectviewmodel.MarginLeft + move),
                            Scene.ToFrame(selectviewmodel.WidthProperty.Value - move + selectviewmodel.MarginLeft),
                            SelectedClip.Layer,
                            out var near))
                        {
                            selectviewmodel.MarginLeft = Scene.ToPixel(near.End);
                            selectviewmodel.WidthProperty.Value = Scene.ToPixel(SelectedClip.End - near.End);

                            return;
                        }

                        // 右
                        selectviewmodel.WidthProperty.Value -= move;
                        selectviewmodel.MarginLeft += move;
                    }
                }

                ClipStartAbs = point;
            }
        }

        public void PointerLeftReleased()
        {
            // マウス押下中フラグを落とす
            SeekbarIsMouseDown = false;
            LayerCursor.Value = StandardCursorType.Arrow;

            // 保存
            if (ClipLeftRight != 0 && SelectedClip != null)
            {
                var clipVm = SelectedClip.GetCreateClipViewModel();

                var length = Scene.ToFrame(clipVm.WidthProperty.Value);

                if (0 < length)
                {
                    var anchor = ClipLeftRight switch
                    {
                        1 => ClipLengthChangeAnchor.End,
                        2 => ClipLengthChangeAnchor.Start,
                        _ => ClipLengthChangeAnchor.Start,
                    };

                    SelectedClip.ChangeLength(anchor, length).Execute();
                }

                ClipLeftRight = 0;
            }
        }

        public void PointerLeftPressed()
        {
            if (ClipMouseDown || !KeyframeToggle)
            {
                return;
            }

            // フラグを"マウス押下中"にする
            SeekbarIsMouseDown = true;

            Scene.PreviewFrame = ClickedFrame;
        }

        public void PointerLeaved()
        {
            SeekbarIsMouseDown = false;

            ClipMouseDown = false;
            LayerCursor.Value = StandardCursorType.Arrow;

            if (ClipTimeChange && SelectedClip is not null)
            {
                var clip = SelectedClip;
                var vm = clip.GetCreateClipViewModel();
                var newlayer = Math.Clamp(vm.Row, 1, 100);
                var newframe = Math.Clamp(Scene.ToFrame(vm.MarginLeft), 0, Scene.TotalFrame);

                clip.MoveFrameLayer(newframe, newlayer).Execute();

                ClipTimeChange = false;
            }
        }
    }
}