using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Abstractions.Serializers;

public interface ISerializer
{
    string ProtocolName { get; }

    WebSocketMessageType MessageType { get; }

    ReadOnlyMemory<byte> Serialize<T>(T message);

    object? Deserialize(Type type, ReadOnlySpan<byte> data);

    int FieldValueIndex(ReadOnlySpan<byte> data, ReadOnlySpan<byte> field);
}
