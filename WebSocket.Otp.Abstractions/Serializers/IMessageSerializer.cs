using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions.Serializers;

public interface IMessageSerializer
{
    string Protocol { get; }

    WebSocketMessageType Type { get; }

    ReadOnlyMemory<byte> Serialize<T>(T message);
    T? Deserialize<T>(ReadOnlySpan<byte> data);

    ReadOnlyMemory<byte> SerializeToMessage<T>(EndpointHeaders headers, T data);
    void ScanMessage(ReadOnlySpan<byte> json, string[] fields, Span<JsonSlice> results);

    bool TryGetFieldValueIndex(ReadOnlySpan<byte> data, string field, out int start);

    bool TryGetFieldValueRange(ReadOnlySpan<byte> data, string field, out int start, out int end);
}

public readonly struct JsonSlice(int start, int end)
{
    public readonly int Start = start;
    public readonly int End = end;
    public readonly bool Found = true;
}