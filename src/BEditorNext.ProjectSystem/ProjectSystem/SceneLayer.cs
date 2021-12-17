﻿using System.Text.Json;
using System.Text.Json.Nodes;

using BEditorNext.Commands;
using BEditorNext.Media;

namespace BEditorNext.ProjectSystem;

public class SceneLayer : Element, IStorable
{
    public static readonly PropertyDefine<TimeSpan> StartProperty;
    public static readonly PropertyDefine<TimeSpan> LengthProperty;
    public static readonly PropertyDefine<int> LayerProperty;
    public static readonly PropertyDefine<Color> AccentColorProperty;
    public static readonly PropertyDefine<bool> IsEnabledProperty;
    private TimeSpan _start;
    private TimeSpan _length;
    private int _layer;
    private string? _fileName;
    private bool _isEnabled = true;

    static SceneLayer()
    {
        StartProperty = RegisterProperty<TimeSpan, SceneLayer>(nameof(Start), (owner, obj) => owner.Start = obj, owner => owner.Start)
            .NotifyPropertyChanging(true)
            .NotifyPropertyChanged(true)
            .JsonName("start");

        LengthProperty = RegisterProperty<TimeSpan, SceneLayer>(nameof(Length), (owner, obj) => owner.Length = obj, owner => owner.Length)
            .NotifyPropertyChanging(true)
            .NotifyPropertyChanged(true)
            .JsonName("length");

        LayerProperty = RegisterProperty<int, SceneLayer>(nameof(Layer), (owner, obj) => owner.Layer = obj, owner => owner.Layer)
            .NotifyPropertyChanging(true)
            .NotifyPropertyChanged(true)
            .JsonName("layer");

        AccentColorProperty = RegisterProperty<Color, SceneLayer>(nameof(AccentColor))
            .DefaultValue(Colors.Teal)
            .NotifyPropertyChanging(true)
            .NotifyPropertyChanged(true)
            .JsonName("accentColor");

        IsEnabledProperty = RegisterProperty<bool, SceneLayer>(nameof(IsEnabled), (owner, obj) => owner.IsEnabled = obj, owner => owner.IsEnabled)
            .DefaultValue(true)
            .NotifyPropertyChanging(true)
            .NotifyPropertyChanged(true)
            .JsonName("isEnabled");
    }

    public SceneLayer()
    {
    }

    public SceneLayer(Scene scene)
        : this()
    {
        Parent = scene;
    }

    // 0以上
    public TimeSpan Start
    {
        get => _start;
        set => SetAndRaise(StartProperty, ref _start, value);
    }

    public TimeSpan Length
    {
        get => _length;
        set => SetAndRaise(LengthProperty, ref _length, value);
    }

    public int Layer
    {
        get => _layer;
        set => SetAndRaise(LayerProperty, ref _layer, value);
    }

    public Color AccentColor
    {
        get => GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetAndRaise(IsEnabledProperty, ref _isEnabled, value);
    }

    public IEnumerable<RenderOperation> Operations => Children.OfType<RenderOperation>();

    public string FileName
    {
        get => _fileName ?? throw new Exception("The file name is not set.");
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _fileName = value;
        }
    }

    public DateTime LastSavedTime { get; private set; }

    public void AddChild(RenderOperation operation, CommandRecorder? recorder = null)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (recorder == null)
        {
            Children.Add(operation);
        }
        else
        {
            recorder.DoAndPush(new AddCommand<Element>(Children, operation, Children.Count));
        }
    }

    public void RemoveChild(RenderOperation operation, CommandRecorder? recorder = null)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (recorder == null)
        {
            Children.Remove(operation);
        }
        else
        {
            recorder.DoAndPush(new RemoveCommand<Element>(Children, operation));
        }
    }

    public void InsertChild(int index, RenderOperation operation, CommandRecorder? recorder = null)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (recorder == null)
        {
            Children.Insert(index, operation);
        }
        else
        {
            recorder.DoAndPush(new AddCommand<Element>(Children, operation, index));
        }
    }

    public void UpdateTime(TimeSpan start, TimeSpan length, CommandRecorder? recorder = null)
    {
        if (start < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(start));

        if (length <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(length));

        if (recorder == null)
        {
            Start = start;
            Length = length;
        }
        else if (start != Start || length != Length)
        {
            recorder.DoAndPush(new UpdateTimeCommand(this, start, Start, length, Length));
        }
    }

    public void Save(string filename)
    {
        _fileName = filename;
        LastSavedTime = DateTime.Now;
        string? directory = Path.GetDirectoryName(_fileName);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Write);
        using var writer = new Utf8JsonWriter(stream, JsonHelper.WriterOptions);

        ToJson().WriteTo(writer, JsonHelper.SerializerOptions);
    }

    public void Restore(string filename)
    {
        _fileName = filename;
        LastSavedTime = DateTime.Now;

        using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
        var node = JsonNode.Parse(stream);

        if (node != null)
        {
            FromJson(node);
        }
    }

    public override void FromJson(JsonNode json)
    {
        base.FromJson(json);

        if (json is JsonObject jobject)
        {
            if (jobject.TryGetPropertyValue("operations", out JsonNode? operationsNode) &&
                operationsNode is JsonArray operationsArray)
            {
                foreach (JsonObject operationJson in operationsArray.OfType<JsonObject>())
                {
                    if (operationJson.TryGetPropertyValue("@type", out JsonNode? atTypeNode) &&
                        atTypeNode is JsonValue atTypeValue &&
                        atTypeValue.TryGetValue(out string? atType))
                    {
                        var type = TypeResolver.ToType(atType);
                        RenderOperation? operation = null;

                        if (type?.IsAssignableTo(typeof(RenderOperation)) ?? false)
                        {
                            operation = Activator.CreateInstance(type) as RenderOperation;
                        }

                        operation ??= new EmptyOperation();
                        operation.FromJson(operationJson);
                        Children.Add(operation);
                    }
                }
            }
        }
    }

    public override JsonNode ToJson()
    {
        JsonNode node = base.ToJson();

        if (node is JsonObject jobject)
        {
            var array = new JsonArray();

            foreach (RenderOperation item in Operations)
            {
                JsonNode json = item.ToJson();
                if (item is not EmptyOperation)
                {
                    json["@type"] = TypeResolver.ToString(item.GetType());
                }

                if (json.Parent != null)
                {
                    json = JsonNode.Parse(json.ToJsonString())!;
                }

                array.Add(json);
            }

            jobject["operations"] = array;
        }

        return node;
    }

    private sealed class UpdateTimeCommand : IRecordableCommand
    {
        private readonly SceneLayer _layer;
        private readonly TimeSpan _newStart;
        private readonly TimeSpan _oldStart;
        private readonly TimeSpan _newLength;
        private readonly TimeSpan _oldLength;

        public UpdateTimeCommand(SceneLayer layer, TimeSpan newStart, TimeSpan oldStart, TimeSpan newLength, TimeSpan oldLength)
        {
            _layer = layer;
            _newStart = newStart;
            _oldStart = oldStart;
            _newLength = newLength;
            _oldLength = oldLength;
        }

        public void Do()
        {
            _layer.Start = _newStart;
            _layer.Length = _newLength;
        }

        public void Redo()
        {
            Do();
        }

        public void Undo()
        {
            _layer.Start = _oldStart;
            _layer.Length = _oldLength;
        }
    }
}
