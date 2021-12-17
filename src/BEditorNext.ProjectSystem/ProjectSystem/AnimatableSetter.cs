﻿using System.Text.Json;
using System.Text.Json.Nodes;

using BEditorNext.Animation;
using BEditorNext.Commands;
using BEditorNext.Collections;

namespace BEditorNext.ProjectSystem;

public interface IAnimatableSetter : ISetter, ILogicalElement
{
    public IReadOnlyList<IAnimation> Children { get; }

    IEnumerable<ILogicalElement> ILogicalElement.LogicalChildren => Children;

    public void SetProperty(TimeSpan progress);

    public void AddChild(IAnimation animation, CommandRecorder? recorder = null);

    public void RemoveChild(IAnimation animation, CommandRecorder? recorder = null);

    public void InsertChild(int index, IAnimation animation, CommandRecorder? recorder = null);
}

public class AnimatableSetter<T> : Setter<T>, IAnimatableSetter
    where T : struct
{
    private readonly ObservableList<Animation<T>> _children = new();

    public AnimatableSetter()
    {
    }

    public AnimatableSetter(PropertyDefine<T> property)
        : base(property)
    {
    }

    public IObservableList<Animation<T>> Children => _children;

    IReadOnlyList<IAnimation> IAnimatableSetter.Children => _children;

    public void SetProperty(TimeSpan progress)
    {
        if (_children.Count < 1)
        {
            Parent.SetValue(Property, Value);
        }
        else
        {
            TimeSpan cur = TimeSpan.Zero;
            for (int i = 0; i < _children.Count; i++)
            {
                Animation<T> item = _children[i];

                TimeSpan next = cur + item.Duration;
                if (cur <= progress && progress < next)
                {
                    // 相対的なTimeSpan
                    TimeSpan time = progress - cur;
                    // イージングする
                    float ease = item.Easing.Ease((float)(time / item.Duration));
                    // 値を補間する
                    T value = item.Animator.Interpolate(ease, item.Previous, item.Next);
                    // 値をセット
                    Parent.SetValue(Property, value);
                    return;
                }
                else
                {
                    cur = next;
                }
            }
        }
    }

    public void AddChild(Animation<T> animation, CommandRecorder? recorder = null)
    {
        ArgumentNullException.ThrowIfNull(animation);

        animation.SetParent(this);
        if (recorder == null)
        {
            Children.Add(animation);
        }
        else
        {
            recorder.DoAndPush(new AddCommand<Animation<T>>(_children, animation, Children.Count));
        }
    }

    public void RemoveChild(Animation<T> animation, CommandRecorder? recorder = null)
    {
        ArgumentNullException.ThrowIfNull(animation);

        if (recorder == null)
        {
            Children.Remove(animation);
        }
        else
        {
            recorder.DoAndPush(new RemoveCommand<Animation<T>>(_children, animation));
        }
    }

    public void InsertChild(int index, Animation<T> animation, CommandRecorder? recorder = null)
    {
        ArgumentNullException.ThrowIfNull(animation);

        animation.SetParent(this);
        if (recorder == null)
        {
            Children.Insert(index, animation);
        }
        else
        {
            recorder.DoAndPush(new AddCommand<Animation<T>>(_children, animation, index));
        }
    }

    public override void FromJson(JsonNode json)
    {
        _children.Clear();
        if (json is JsonObject jsonobj)
        {
            if (jsonobj.TryGetPropertyValue("children", out JsonNode? childrenNode) &&
                childrenNode is JsonArray jsonArray)
            {
                foreach (JsonNode? item in jsonArray)
                {
                    if (item is JsonObject jobj)
                    {
                        var anm = new Animation<T>();
                        anm.SetParent(this);
                        anm.FromJson(jobj);
                        _children.Add(anm);
                    }
                }
            }

            if (jsonobj.TryGetPropertyValue("value", out JsonNode? valueNode))
            {
                T? value = JsonSerializer.Deserialize<T>(valueNode, JsonHelper.SerializerOptions);
                if (value != null)
                    Value = (T)value;
            }
        }
    }

    public override JsonNode ToJson()
    {
        var jsonObj = new JsonObject();
        var jsonArray = new JsonArray();
        foreach (Animation<T> item in _children)
        {
            JsonNode? json = item.ToJson();

            if (json.Parent != null)
            {
                json = JsonNode.Parse(json.ToJsonString())!;
            }

            jsonArray.Add(json);
        }

        jsonObj["value"] = JsonSerializer.SerializeToNode(Value, JsonHelper.SerializerOptions);
        jsonObj["children"] = jsonArray;

        return jsonObj;
    }

    void IAnimatableSetter.AddChild(IAnimation animation, CommandRecorder? recorder)
    {
        AddChild((Animation<T>)animation, recorder);
    }

    void IAnimatableSetter.RemoveChild(IAnimation animation, CommandRecorder? recorder)
    {
        RemoveChild((Animation<T>)animation, recorder);
    }

    void IAnimatableSetter.InsertChild(int index, IAnimation animation, CommandRecorder? recorder)
    {
        InsertChild(index, (Animation<T>)animation, recorder);
    }
}
