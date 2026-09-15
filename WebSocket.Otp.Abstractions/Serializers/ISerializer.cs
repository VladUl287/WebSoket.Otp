using System.Net.WebSockets;

namespace WebSockets.Otp.Abstractions.Serializers;

public interface ISerializer
{
    string Protocol { get; }

    WebSocketMessageType Type { get; }

    ReadOnlyMemory<byte> Serialize<T>(T message);

    T? Deserialize<T>(ReadOnlySpan<byte> data);

    bool TryGetFieldValueIndex(ReadOnlySpan<byte> data, string field, out int index);
}
