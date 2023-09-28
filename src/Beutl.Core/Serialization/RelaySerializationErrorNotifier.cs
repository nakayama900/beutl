﻿namespace Beutl.Serialization;

public sealed class RelaySerializationErrorNotifier : ISerializationErrorNotifier
{
    public RelaySerializationErrorNotifier(ISerializationErrorNotifier parent, string? prependPath)
    {
        Parent = parent;
        Prepend = prependPath;
    }

    public ISerializationErrorNotifier Parent { get; }

    public string? Prepend { get; }

    public void NotifyError(string path, string message, Exception? ex = null)
    {
        if (Parent is RelaySerializationErrorNotifier relay)
        {
            var pathSegments = new List<string>() { path };
            if (!string.IsNullOrWhiteSpace(Prepend))
            {
                pathSegments.Insert(0, Prepend);
            }

            relay.NotifyError(pathSegments, message, ex);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(Prepend))
            {
                Parent.NotifyError($"{Prepend}.{path}", message, ex);
            }
            else
            {
                Parent.NotifyError(path, message, ex);
            }
        }
    }

    private void NotifyError(List<string> pathSegments, string message, Exception? ex = null)
    {
        if (Parent is RelaySerializationErrorNotifier relay)
        {
            if (!string.IsNullOrWhiteSpace(Prepend))
            {
                pathSegments.Insert(0, Prepend);
            }

            relay.NotifyError(pathSegments, message, ex);
        }
        else
        {
            Parent.NotifyError(string.Concat(pathSegments), message, ex);
        }
    }
}
