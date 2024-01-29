﻿using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Beutl.Configuration;
using Beutl.Media;
using Beutl.Media.Pixel;
using Beutl.Media.Source;

using OpenCvSharp;

using Reactive.Bindings;

namespace Beutl.Models;

public sealed partial class FrameCacheManager : IDisposable
{
    private readonly SortedDictionary<int, CacheEntry> _entries = [];
    private readonly object _lock = new();
    private readonly PixelSize _frameSize;
    private readonly ReadOnlyReactivePropertySlim<long> _maxSize;
    private long _size;

    public event Action<ImmutableArray<CacheBlock>>? BlocksUpdated;

    public FrameCacheManager(PixelSize frameSize, FrameCacheOptions options)
    {
        _frameSize = frameSize;
        Options = options;

        _maxSize = GlobalConfiguration.Instance.EditorConfig.GetObservable(EditorConfig.FrameCacheMaxSizeProperty)
            .Select(v => (long)(v * 1024 * 1024))
            .ToReadOnlyReactivePropertySlim();
    }

    public ImmutableArray<CacheBlock> Blocks { get; private set; }

    public FrameCacheOptions Options { get; set; }

    // 再生中のフレーム
    public int CurrentFrame { get; set; }

    public void Add(int frame, Ref<Bitmap<Bgra8888>> bitmap)
    {
        lock (_lock)
        {
            if (_entries.TryGetValue(frame, out CacheEntry? old))
            {
                if (!old.IsLocked)
                    old.SetBitmap(bitmap, Options);
            }
            else
            {
                var entry = new CacheEntry(bitmap, Options);
                _size += entry.ByteCount;
                _entries.Add(frame, entry);
            }
        }

        if (_size >= _maxSize.Value)
        {
            Task.Run(AutoDelete);
        }
    }

    public bool TryGet(int frame, [MaybeNullWhen(false)] out Ref<Bitmap<Bgra8888>> bitmap)
    {
        lock (_lock)
        {
            if (_entries.TryGetValue(frame, out CacheEntry? e))
            {
                bitmap = e.GetBitmap();
                return true;
            }
            else
            {
                bitmap = null;
                return false;
            }
        }
    }

    public bool RemoveRange(int start, int end)
    {
        lock (_lock)
        {
            int[] keys = _entries.Where(v => !v.Value.IsLocked)
                .Select(p => p.Key)
                .SkipWhile(t => t < start)
                .TakeWhile(t => t < end)
                .ToArray();

            foreach (int key in keys)
            {
                if (_entries.Remove(key, out CacheEntry? e))
                {
                    _size -= e.ByteCount;
                    e.Dispose();
                }
            }

            return keys.Length > 0;
        }
    }

    public void Lock(int start, int end)
    {
        lock (_lock)
        {
            foreach (KeyValuePair<int, CacheEntry> item in _entries
                .SkipWhile(t => t.Key < start)
                .TakeWhile(t => t.Key < end))
            {
                if (!item.Value.IsLocked)
                {
                    _size -= item.Value.ByteCount;
                }

                item.Value.IsLocked = true;
            }
        }
    }

    public void Unlock(int start, int end)
    {
        lock (_lock)
        {
            foreach (KeyValuePair<int, CacheEntry> item in _entries
                .SkipWhile(t => t.Key < start)
                .TakeWhile(t => t.Key < end))
            {
                if (item.Value.IsLocked)
                {
                    _size += item.Value.ByteCount;
                }

                item.Value.IsLocked = false;
            }
        }
    }

    public long CalculateByteCount(int start, int end)
    {
        lock (_lock)
        {
            return _entries
                .SkipWhile(t => t.Key < start)
                .TakeWhile(t => t.Key < end)
                .Sum(t => (long)t.Value.ByteCount);
        }
    }

    public void RemoveAndUpdateBlocks(IEnumerable<(int Start, int End)> timeRanges)
    {
        lock (_lock)
        {
            bool removedAnyCache = false;

            foreach ((int Start, int End) in timeRanges)
            {
                removedAnyCache |= RemoveRange(Start, End);
            }

            if (removedAnyCache)
            {
                UpdateBlocks();
            }
        }
    }

    public void Dispose()
    {
        Clear();
        _maxSize.Dispose();
    }

    public void Clear()
    {
        lock (_lock)
        {
            int[] keys = [.. _entries.Keys];
            foreach (CacheEntry item in _entries.Values)
            {
                item.Dispose();
            }

            _size = 0;
            _entries.Clear();
            BlocksUpdated?.Invoke([]);
        }
    }

    private void AutoDelete()
    {
        KeyValuePair<int, CacheEntry>[] GetOldCaches(long targetCount)
        {
            return _entries
                .Where(v => !v.Value.IsLocked)
                .OrderBy(v => v.Value.LastAccessTime)
                .Take((int)targetCount)
                .ToArray();
        }

        KeyValuePair<int, CacheEntry>[] GetFarCaches(long targetCount)
        {
            return _entries
                .Where(v => !v.Value.IsLocked && v.Key < CurrentFrame)
                .OrderBy(v => v.Key - CurrentFrame)
                .Take((int)targetCount)
                .ToArray();
        }

        void DeleteItems(KeyValuePair<int, CacheEntry>[] items)
        {
            foreach (KeyValuePair<int, CacheEntry> item in items)
            {
                if (_size < _maxSize.Value)
                    break;

                _size -= item.Value.ByteCount;
                item.Value.Dispose();
                _entries.Remove(item.Key);
            }
        }

        void DeleteBackwardBlock()
        {
            ImmutableArray<CacheBlock> blocks = CalculateBlocks(int.MinValue, CurrentFrame);
            CacheBlock? skip = null;

            foreach (CacheBlock? item in blocks.Where(v => !v.IsLocked)
                .OrderByDescending(b => b.Length)
                .ToArray())
            {
                if (item.Start + item.Length < CurrentFrame)
                {
                    skip = item;
                }

                RemoveRange(item.Start, item.Start + item.Length);
                if (_size < _maxSize.Value)
                    return;
            }

            if (skip != null)
            {
                RemoveRange(skip.Start, skip.Start + skip.Length - 1);
            }
        }

        lock (_lock)
        {
            int loop = 5;
            FrameCacheDeletionStrategy strategy = Options.DeletionStrategy;

            while (_size >= _maxSize.Value && loop >= 0)
            {
                if (strategy == FrameCacheDeletionStrategy.BackwardBlock)
                {
                    DeleteBackwardBlock();
                    strategy = FrameCacheDeletionStrategy.Far;
                    if (_size < _maxSize.Value)
                    {
                        return;
                    }
                }

                long excess = _size - _maxSize.Value;
                int sizePerCache = CalculateBitmapByteSize(Options.GetSize(_frameSize), Options.ColorType == FrameCacheColorType.YUV);
                long targetCount = excess / sizePerCache;

                var items = Options.DeletionStrategy == FrameCacheDeletionStrategy.Old
                    ? GetOldCaches(targetCount)
                    : GetFarCaches(targetCount);
                DeleteItems(items);

                loop--;
            }
        }
    }

    private static int CalculateBitmapByteSize(PixelSize size, bool i420)
    {
        return i420 ? size.Width * (int)(size.Height * 1.5)
            : size.Width * size.Height * 4;
    }
}
