﻿using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

using Beutl.Collections;
using Beutl.Framework;

using Microsoft.Extensions.DependencyInjection;

namespace Beutl.ProjectSystem;

public static class ProjectVariableKeys
{
    public const string FrameRate = "framerate";
    public const string SampleRate = "samplerate";
}

// Todo: IResourceProviderを実装
public sealed class Project : Hierarchical, IStorable, IWorkspace
{
    public static readonly CoreProperty<Version> AppVersionProperty;
    public static readonly CoreProperty<Version> MinAppVersionProperty;
    private string? _rootDirectory;
    private string? _fileName;
    private EventHandler? _saved;
    private EventHandler? _restored;
    private readonly HierarchicalList<IWorkspaceItem> _items;
    private readonly Dictionary<string, string> _variables = new();

    static Project()
    {
        AppVersionProperty = ConfigureProperty<Version, Project>(nameof(AppVersion))
            .Accessor(o => o.AppVersion)
            .Register();

        MinAppVersionProperty = ConfigureProperty<Version, Project>(nameof(MinAppVersion))
            .Accessor(o => o.MinAppVersion)
            .DefaultValue(new Version(0, 3))
            .Register();
    }

    public Project()
    {
        MinAppVersion = new Version(0, 3);
        _items = new HierarchicalList<IWorkspaceItem>(this);
        _items.CollectionChanged += Items_CollectionChanged;
    }

    event EventHandler IStorable.Saved
    {
        add => _saved += value;
        remove => _saved -= value;
    }

    event EventHandler IStorable.Restored
    {
        add => _restored += value;
        remove => _restored -= value;
    }

    public string RootDirectory => _rootDirectory ?? throw new Exception("The file name is not set.");

    public string FileName => _fileName ?? throw new Exception("The file name is not set.");

    public Version AppVersion { get; private set; } = Assembly.GetEntryAssembly()!.GetName().Version ?? new Version();

    public Version MinAppVersion { get; private set; }

    public DateTime LastSavedTime { get; private set; }

    public ICoreList<IWorkspaceItem> Items => _items;

    public IDictionary<string, string> Variables => _variables;

    public void Restore(string filename)
    {
        _fileName = filename;
        _rootDirectory = Path.GetDirectoryName(filename);

        this.JsonRestore(filename);
        LastSavedTime = File.GetLastWriteTimeUtc(filename);

        _restored?.Invoke(this, EventArgs.Empty);
    }

    public void Save(string filename)
    {
        _fileName = filename;
        _rootDirectory = Path.GetDirectoryName(filename);
        LastSavedTime = DateTime.UtcNow;

        this.JsonSave(filename);
        File.SetLastWriteTimeUtc(filename, LastSavedTime);

        _saved?.Invoke(this, EventArgs.Empty);
    }

    public override void ReadFromJson(JsonNode json)
    {
        base.ReadFromJson(json);

        if (json is JsonObject jobject)
        {
            if (jobject.TryGetPropertyValue("appVersion", out JsonNode? versionNode)
                && versionNode!.AsValue().TryGetValue(out Version? version))
            {
                AppVersion = version;
            }

            if (jobject.TryGetPropertyValue("minAppVersion", out JsonNode? minVersionNode)
                && minVersionNode!.AsValue().TryGetValue(out Version? minVersion))
            {
                MinAppVersion = minVersion;
            }

            if (jobject.TryGetPropertyValue("items", out JsonNode? itemsNode))
            {
                SyncronizeScenes(itemsNode!.AsArray()
                    .Select(i => (string)i!));
            }

            if (jobject.TryGetPropertyValue("variables", out JsonNode? variablesNode)
                && variablesNode is JsonObject variablesObj)
            {
                Variables.Clear();
                foreach (KeyValuePair<string, JsonNode?> item in variablesObj)
                {
                    if (item.Value != null)
                        Variables[item.Key] = item.Value.AsValue().ToString();
                }
            }
        }
    }

    public override void WriteToJson(ref JsonNode json)
    {
        base.WriteToJson(ref json);

        if (json is JsonObject jobject)
        {
            jobject["appVersion"] = JsonValue.Create(AppVersion);
            jobject["minAppVersion"] = JsonValue.Create(MinAppVersion);

            var items = new JsonArray();
            foreach (IWorkspaceItem item in Items)
            {
                string path = Path.GetRelativePath(RootDirectory, item.FileName).Replace('\\', '/');
                var value = JsonValue.Create(path);
                items.Add(value);
            }

            jobject["items"] = items;

            var variables = new JsonObject();
            foreach (KeyValuePair<string, string> item in Variables)
            {
                variables.Add(item.Key, JsonValue.Create(item.Value));
            }

            jobject["variables"] = variables;
        }
    }

    public void Dispose()
    {
        _items.CollectionChanged -= Items_CollectionChanged;
        _items.Clear();
    }

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_fileName != null)
            Save(_fileName);
    }

    private void SyncronizeScenes(IEnumerable<string> pathToItem)
    {
        _items.CollectionChanged -= Items_CollectionChanged;
        pathToItem = pathToItem.Select(x => Path.GetFullPath(x, RootDirectory)).ToArray();

        // 削除するシーン
        IEnumerable<IWorkspaceItem> toRemoveItems = _items.ExceptBy(pathToItem, x => x.FileName);
        // 追加するシーン
        IEnumerable<string> toAddItems = pathToItem.Except(_items.Select(x => x.FileName));

        foreach (IWorkspaceItem? item in toRemoveItems)
        {
            _items.Remove(item);
        }

        IWorkspaceItemContainer resolver = ServiceLocator.Current.GetRequiredService<IWorkspaceItemContainer>();
        foreach (string item in toAddItems)
        {
            if (resolver.TryGetOrCreateItem(item, out IWorkspaceItem? workspaceItem))
            {
                _items.Add(workspaceItem);
            }
        }

        _items.CollectionChanged += Items_CollectionChanged;
    }
}
