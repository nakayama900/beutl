﻿using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

using Beutl.Graphics.Transformation;
using Beutl.Services;
using Beutl.ViewModels.Editors;

using FluentAvalonia.UI.Controls;

namespace Beutl.Views.Editors;

public partial class TransformEditor : UserControl
{
    private static readonly CrossFade s_transition = new(TimeSpan.FromMilliseconds(250));
    private CancellationTokenSource? _lastTransitionCts;

    private static FAMenuFlyout? s_flyout;
    private static EventHandler<RoutedEventArgs>? s_handler;

    public TransformEditor()
    {
        InitializeComponent();
        expandToggle.GetObservable(ToggleButton.IsCheckedProperty)
            .Subscribe(async v =>
            {
                _lastTransitionCts?.Cancel();
                _lastTransitionCts = new CancellationTokenSource();
                CancellationToken localToken = _lastTransitionCts.Token;

                if (v == true)
                {
                    await s_transition.Start(null, content, localToken);
                }
                else
                {
                    await s_transition.Start(content, null, localToken);
                }
            });

        ChangeTypeMenu.ItemsSource = CreateMenuItems(TransformTypeClicked);

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        KnownTransformType ToKnownType(Type type)
        {
            if (type == typeof(TransformGroup))
                return KnownTransformType.Group;
            else if (type == typeof(TranslateTransform))
                return KnownTransformType.Translate;
            else if (type == typeof(RotationTransform))
                return KnownTransformType.Rotation;
            else if (type == typeof(ScaleTransform))
                return KnownTransformType.Scale;
            else if (type == typeof(SkewTransform))
                return KnownTransformType.Skew;
            else if (type == typeof(Rotation3DTransform))
                return KnownTransformType.Rotation3D;
            else
                return KnownTransformType.Unknown;
        }

        if (e.Data.Get(KnownLibraryItemFormats.Transform) is Type type
            && DataContext is TransformEditorViewModel viewModel)
        {
            KnownTransformType knownType = ToKnownType(type);
            if (knownType == KnownTransformType.Unknown)
                return;

            if (viewModel.IsGroup.Value)
            {
                viewModel.AddItem(knownType);
            }
            else
            {
                viewModel.ChangeType(knownType);
            }

            viewModel.IsExpanded.Value = true;
            e.Handled = true;
        }
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(KnownLibraryItemFormats.Transform))
        {
            e.DragEffects = DragDropEffects.Copy | DragDropEffects.Link;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private static MenuFlyoutItem[] CreateMenuItems(EventHandler<RoutedEventArgs>? handler)
    {
        var items = new (KnownTransformType Tag, string Name, string? Icon)[]
        {
            (KnownTransformType.Group,      "Group",       null),
            (KnownTransformType.Translate,  "Translate",   "TranslateTransformIconData"),
            (KnownTransformType.Rotation,   "Rotation",    "RotationTransformIconData"),
            (KnownTransformType.Scale,      "Scale",       "ScaleTransformIconData"),
            (KnownTransformType.Skew,       "Skew",        "SkewTransformIconData"),
            (KnownTransformType.Rotation3D, "Rotation 3D", "Rotation3DTransformIconData"),
        };
        return items.Select(x =>
            {
                var obj = new MenuFlyoutItem()
                {
                    Tag = x.Tag,
                    Text = x.Name
                };
                if (handler != null)
                {
                    obj.Click += handler;
                }

                if (x.Icon != null)
                {
                    obj.IconSource = new PathIconSource
                    {
                        Data = Application.Current?.FindResource(x.Icon) as Avalonia.Media.Geometry
                    };
                }

                return obj;
            })
            .ToArray();
    }

    private static FAMenuFlyout GetOrCreateFlyout()
    {
        return s_flyout ??= new FAMenuFlyout()
        {
            ItemsSource = CreateMenuItems((s, e) => s_handler?.Invoke(s, e))
        };
    }

    private void Tag_Click(object? sender, RoutedEventArgs e)
    {
        void Flyout_Closed(object? sender, EventArgs e)
        {
            if (sender is FAMenuFlyout flyout)
            {
                flyout.Closed -= Flyout_Closed;
                s_handler -= AddTransformClick;
            }
        }

        if (DataContext is TransformEditorViewModel viewModel)
        {
            if (viewModel.IsGroup.Value)
            {
                FAMenuFlyout flyout = GetOrCreateFlyout();
                flyout.ShowAt(this);

                s_handler += AddTransformClick;
                flyout.Closed += Flyout_Closed;
            }
            else
            {
                expandToggle.ContextFlyout?.ShowAt(expandToggle);
            }
        }
    }

    private void AddTransformClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: KnownTransformType type }
            && DataContext is TransformEditorViewModel viewModel)
        {
            viewModel.AddItem(type);
        }
    }

    private void TransformTypeClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: KnownTransformType type }
            && DataContext is TransformEditorViewModel viewModel)
        {
            viewModel.ChangeType(type);
        }
    }

    private void SetNullClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TransformEditorViewModel viewModel)
        {
            viewModel.SetNull();
        }
    }
}
