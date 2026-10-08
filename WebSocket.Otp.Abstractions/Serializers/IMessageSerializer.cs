using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Abstractions.Serializers;

public interface IMessageSerializer
{
    string Protocol { get; }

    WebSocketMessageType Type { get; }

    ReadOnlyMemory<byte> Serialize<T>(T message);
    T? Deserialize<T>(ReadOnlySpan<byte> data);

    uint ParseUInt(ReadOnlySpan<byte> data);
    ReadOnlyMemory<byte> SerializeToMessage<T>(EndpointHeaders headers, T data);
    void ScanMessage(ReadOnlySpan<byte> json, Span<JsonSlice> results);
}
