namespace WebSockets.Otp.Abstractions.Options;

public readonly record struct MessageHeaders(uint Id, ReadOnlyMemory<byte> Key, ReadOnlyMemory<byte> Value);

public enum MessageType
{
    Default,
    Error
}

public readonly record struct MessageEnvelope<T>(string Key, T Value, uint Id = default, MessageType Type = default);
