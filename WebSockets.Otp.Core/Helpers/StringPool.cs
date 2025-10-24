using System.Buffers;
using System.Collections.Frozen;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Helpers;

public sealed class StringPool : IStringPool
{
    private readonly FrozenDictionary<ulong, string> _map;
    private readonly Encoding _encoding;

    public StringPool(IEnumerable<string> knownStrings, Encoding encoding)
    {
        _encoding = encoding;

        var map = new Dictionary<ulong, string>();
        foreach (var precomputed in knownStrings)
        {
            var precomputedBytes = _encoding.GetBytes(precomputed);
            var hashCode = GetHashCode(precomputedBytes);

            if (map.TryGetValue(hashCode, out var stored))
                throw new InvalidOperationException($"Collision detected for: {precomputed} and {stored}");

            map[hashCode] = precomputed;
        }

        _map = map.ToFrozenDictionary();
    }

    public string Get(ReadOnlySpan<byte> bytes)
    {
        var hashCode = GetHashCode(bytes);

        if (_map.TryGetValue(hashCode, out var internedValue))
            return internedValue;

        return _encoding.GetString(bytes);
    }

    public string Get(ReadOnlySequence<byte> bytes)
    {
        if (bytes.IsSingleSegment)
            return Get(bytes.FirstSpan);

        var hashCode = GetHashCode(bytes);

        if (_map.TryGetValue(hashCode, out var internedValue))
            return internedValue;

        return _encoding.GetString(bytes);
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
