using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Text;

namespace WebSockets.Otp.Core.Helpers;

public static class StringIntern
{
    private static readonly ConcurrentDictionary<ulong, string> map = new();

    public static string Intern(ReadOnlySpan<byte> utf8Bytes)
    {
        var hashCode = XxHash64.HashToUInt64(utf8Bytes);

        if (map.TryGetValue(hashCode, out var internedValue))
            return internedValue;

        internedValue = Encoding.UTF8.GetString(utf8Bytes);
        map[hashCode] = internedValue;
        return internedValue;
    }
}
