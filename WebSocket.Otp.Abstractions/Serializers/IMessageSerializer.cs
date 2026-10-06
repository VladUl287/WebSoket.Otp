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
    void ScanMessage(ReadOnlySpan<byte> json, Span<JsonSlice> results);
}

public readonly struct JsonSlice(int start, int end)
{
    public readonly int Start = start;
    public readonly int End = end;
    public readonly bool Found = true;
}