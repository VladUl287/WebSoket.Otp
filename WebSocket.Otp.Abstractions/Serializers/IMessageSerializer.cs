using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Models;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Abstractions.Serializers;

public interface IMessageSerializer
{
    string Protocol { get; }
    WebSocketMessageType Type { get; }

    T? Deserialize<T>(ReadOnlySpan<byte> data);

    uint ParseUInt(ReadOnlySpan<byte> data);
    void ScanMessage(ReadOnlySpan<byte> data, Span<Segment> slices);
    ReadOnlyMemory<byte> SerializeToMessage<T>(MessageHeaders headers, T data);
}
