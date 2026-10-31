using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Serializers;

public interface IMessageSerializer
{
    string Protocol { get; }
    WebSocketMessageType Type { get; }

    T? Deserialize<T>(ReadOnlyMemory<byte> data);

    MessageHeaders ReadHeaders(ReadOnlyMemory<byte> data);
    ReadOnlyMemory<byte> SerializeToMessage<T>(MessageEnvelope<T> data);
}
