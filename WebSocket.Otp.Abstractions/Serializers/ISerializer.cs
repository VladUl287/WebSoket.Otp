using System.Net.WebSockets;

namespace WebSockets.Otp.Abstractions.Serializers;

public interface ISerializer
{
    string ProtocolName { get; }

    WebSocketMessageType MessageType { get; }

    ReadOnlyMemory<byte> Serialize<T>(T message);

    object? Deserialize(Type type, ReadOnlySpan<byte> data);

    bool TryGetFieldValueIndex(ReadOnlySpan<byte> data, string fieldName, out int index);
}
