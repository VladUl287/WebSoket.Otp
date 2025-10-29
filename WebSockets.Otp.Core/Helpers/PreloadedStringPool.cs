﻿using System.Buffers;
using System.Collections.Frozen;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Helpers;

public sealed class Entry
{
    public string Value { get; init; } = string.Empty;
    public byte[] Bytes { get; init; } = [];
    public Entry? Next { get; set; }
}

public sealed class PreloadedStringPool : IStringPool
{
    private readonly FrozenDictionary<ulong, Entry> _mapEntries;
    private readonly Encoding _encoding;
    private readonly bool _hasCollisions;
    private readonly bool _unsafeMode;

    public Encoding Encoding => _encoding;
    public bool HasCollisions => _hasCollisions;

    public PreloadedStringPool(IEnumerable<string> knownStrings, Encoding encoding, bool unsafeMode = false)
    {
        _encoding = encoding;
        _unsafeMode = unsafeMode;

        var uniqueStrings = new HashSet<string>(knownStrings);
        var entries = new Dictionary<ulong, Entry>(uniqueStrings.Count);
        var hasCollisions = false;

        foreach (var str in uniqueStrings)
        {
            var bytes = _encoding.GetBytes(str);
            var hashCode = GetHashCode(bytes);

            var entry = new Entry
            {
                Value = str,
                Bytes = bytes,
            };

            if (entries.TryGetValue(hashCode, out Entry? existingEntry))
            {
                hasCollisions = true;
                entry.Next = existingEntry;
            }

            entries[hashCode] = entry;
        }

        _hasCollisions = hasCollisions;
        _mapEntries = entries.ToFrozenDictionary();
    }

    public string Intern(ReadOnlySpan<byte> bytes)
    {
        var hashCode = GetHashCode(bytes);

        if (!_mapEntries.TryGetValue(hashCode, out var entry))
            return _encoding.GetString(bytes);

        if (_unsafeMode && entry.Next is null)
            return entry.Value;

        return FindExactMatch(bytes, entry) ?? _encoding.GetString(bytes);
    }

    public string Intern(ReadOnlySequence<byte> bytes)
    {
        if (bytes.IsSingleSegment)
            return Intern(bytes.FirstSpan);

        var hashCode = GetHashCode(bytes);

        if (!_mapEntries.TryGetValue(hashCode, out var entry))
            return _encoding.GetString(bytes);

        if (_unsafeMode && entry.Next is null)
            return entry.Value;

        return FindExactMatch(bytes, entry) ?? _encoding.GetString(bytes);
    }

    private static string? FindExactMatch(ReadOnlySpan<byte> bytes, Entry entry)
    {
        Entry? currnet = entry;
        while (currnet is not null)
        {
            if (bytes.SequenceEqual(currnet.Bytes))
                return currnet.Value;

            currnet = currnet.Next;
        }
        return null;
    }

    private static string? FindExactMatch(ReadOnlySequence<byte> bytes, Entry entry)
    {
        Entry? current = entry;
        while (current is not null)
        {
            if (SequenceEqual(bytes, current.Bytes))
                return current.Value;

            current = current.Next;
        }
        return null;
    }

    private static bool SequenceEqual(ReadOnlySequence<byte> first, ReadOnlySpan<byte> second)
    {
        if (first.Length != second.Length)
            return false;

        var secondPosition = 0;

        foreach (var segment in first)
        {
            var segmentData = segment.Span;
            var segmentLength = segmentData.Length;

            if (segmentLength == 0)
                continue;

            var correspondingPart = second.Slice(secondPosition, segmentLength);

            if (!segmentData.SequenceEqual(correspondingPart))
                return false;

            secondPosition += segmentLength;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetHashCode(ReadOnlySpan<byte> bytes) => XxHash3.HashToUInt64(bytes);

    private static ulong GetHashCode(ReadOnlySequence<byte> bytes)
    {
        if (bytes.IsSingleSegment)
            return GetHashCode(bytes.FirstSpan);

        var hasher = new XxHash3();
        foreach (var segment in bytes)
            hasher.Append(segment.Span);
        return hasher.GetCurrentHashAsUInt64();
    }
}
