﻿using System.Collections;
using System.Collections.Specialized;

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

using Beutl.Commands;
using Beutl.Framework;
using Beutl.NodeTree;
using Beutl.NodeTree.Nodes.Group;
using Beutl.Services;

using FluentAvalonia.UI.Media;

using Reactive.Bindings;

namespace Beutl.ViewModels.NodeTree;

public sealed class NodeViewModel : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly string _defaultName;

    public NodeViewModel(Node node)
    {
        Node = node;
        Type nodeType = node.GetType();
        if (NodeRegistry.FindItem(nodeType) is { } regItem)
        {
            _defaultName = regItem.DisplayName;

            var color = new Color2(regItem.AccentColor.ToAvalonia());
            Color = new ImmutableLinearGradientBrush(
                new[]
                {
                    new ImmutableGradientStop(0, color.WithAlphaf(0.1f)),
                    new ImmutableGradientStop(1, color.WithAlphaf(0.01f))
                },
                startPoint: RelativePoint.TopLeft,
                endPoint: RelativePoint.BottomRight);
        }
        else
        {
            _defaultName = nodeType.Name;
            Color = Brushes.Transparent;
        }

        NodeName = node.GetObservable(CoreObject.NameProperty)
            .Select(x => string.IsNullOrWhiteSpace(x) ? _defaultName : x)
            .ToReadOnlyReactiveProperty()
            .DisposeWith(_disposables)!;

        IsExpanded = node.GetObservable(Node.IsExpandedProperty)
            .ToReactiveProperty()
            .DisposeWith(_disposables);

        IsExpanded.Subscribe(v => Node.IsExpanded = v)
            .DisposeWith(_disposables);

        Position = node.GetObservable(Node.PositionProperty)
            .Select(x => new Point(x.X, x.Y))
            .ToReactiveProperty()
            .DisposeWith(_disposables);

        Delete.Subscribe(() =>
        {
            NodeTreeSpace? tree = Node.FindHierarchicalParent<NodeTreeSpace>();
            if (tree != null)
            {
                new RemoveCommand<Node>(tree.Nodes, Node)
                    .DoAndRecord(CommandRecorder.Default);
            }
        });

        InitItems();
    }

    public Node Node { get; }

    public ReadOnlyReactiveProperty<string> NodeName { get; }

    public IBrush Color { get; }

    public ReactiveProperty<bool> IsSelected { get; } = new();

    public bool IsGroupNode => Node is GroupNode;

    public ReactiveProperty<Point> Position { get; }

    public ReactiveProperty<bool> IsExpanded { get; }

    public ReactiveCommand Delete { get; } = new();

    public CoreList<NodeItemViewModel> Items { get; } = new();

    public void Dispose()
    {
        Node.Items.CollectionChanged -= OnItemsCollectionChanged;
        foreach (NodeItemViewModel item in Items)
        {
            item.Dispose();
        }
        Items.Clear();
        Position.Dispose();
        _disposables.Dispose();
    }

    private void InitItems()
    {
        var ctmp = new CoreProperty[1];
        var atmp = new IAbstractProperty[1];
        foreach (INodeItem item in Node.Items)
        {
            Items.Add(CreateNodeItemViewModel(ctmp, atmp, item));
        }

        Node.Items.CollectionChanged += OnItemsCollectionChanged;
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        void Add(int index, IList items)
        {
            var ctmp = new CoreProperty[1];
            var atmp = new IAbstractProperty[1];
            foreach (INodeItem item in items)
            {
                Items.Insert(index++, CreateNodeItemViewModel(ctmp, atmp, item));
            }
        }

        void Remove(int index, IList items)
        {
            for (int i = 0; i < items.Count; ++i)
            {
                Items[index + i].Dispose();
            }

            Items.RemoveRange(index, items.Count);
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                Add(e.NewStartingIndex, e.NewItems!);
                break;

            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Replace:
                Remove(e.OldStartingIndex, e.OldItems!);
                Add(e.NewStartingIndex, e.NewItems!);
                break;

            case NotifyCollectionChangedAction.Remove:
                Remove(e.OldStartingIndex, e.OldItems!);
                break;

            case NotifyCollectionChangedAction.Reset:
                foreach (NodeItemViewModel item in Items)
                {
                    item.Dispose();
                }
                Items.Clear();
                break;
        }
    }

    private NodeItemViewModel CreateNodeItemViewModel(CoreProperty[] ctmp, IAbstractProperty[] atmp, INodeItem item)
    {
        IPropertyEditorContext? context = null;
        if (item.Property is { } aproperty)
        {
            ctmp[0] = aproperty.Property;
            atmp[0] = aproperty;
            (_, PropertyEditorExtension ext) = PropertyEditorService.MatchProperty(ctmp);
            ext?.TryCreateContextForNode(atmp, out context);
        }

        return CreateNodeItemViewModelCore(item, context);
    }

    private NodeItemViewModel CreateNodeItemViewModelCore(INodeItem nodeItem, IPropertyEditorContext? propertyEditorContext)
    {
        return nodeItem switch
        {
            IOutputSocket osocket => new OutputSocketViewModel(osocket, propertyEditorContext, Node),
            IInputSocket isocket => new InputSocketViewModel(isocket, propertyEditorContext, Node),
            ISocket socket => new SocketViewModel(socket, propertyEditorContext, Node),
            _ => new NodeItemViewModel(nodeItem, propertyEditorContext, Node),
        };
    }

    public void UpdatePosition(IEnumerable<NodeViewModel> selection)
    {
        static IRecordableCommand CreateCommand(NodeViewModel viewModel)
        {
            return new ChangePropertyCommand<(double, double)>(
                viewModel.Node,
                Node.PositionProperty,
                (viewModel.Position.Value.X, viewModel.Position.Value.Y),
                viewModel.Node.Position);
        }

        selection.Select(CreateCommand)
            .Append(CreateCommand(this))
            .ToArray()
            .ToCommand()
            .DoAndRecord(CommandRecorder.Default);
    }

    public void UpdateName(string? name)
    {
        new ChangePropertyCommand<string>(Node, CoreObject.NameProperty, name, Node.Name)
            .DoAndRecord(CommandRecorder.Default);
    }
}
