using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Models;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Abstractions.Serializers;

public interface IMessageSerializer
{
    string Protocol { get; }
    WebSocketMessageType Type { get; }

    T? Deserialize<T>(ReadOnlyMemory<byte> data);

    uint ParseUInt(ReadOnlyMemory<byte> data);
    void ScanMessage(ReadOnlyMemory<byte> data, Span<Segment> segments);
    ReadOnlyMemory<byte> SerializeToMessage<T>(MessageHeaders headers, T data);
}
