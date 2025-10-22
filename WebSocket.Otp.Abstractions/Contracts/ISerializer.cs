namespace WebSockets.Otp.Abstractions.Contracts;

public interface ISerializer
{
    string Format { get; }

    ReadOnlyMemory<byte> Serialize<T>(T message) where T : IWsMessage;

    T? Deserialize<T>(ReadOnlyMemory<byte> jsonUtf8) where T : class, IWsMessage;

    object Deserialize(Type type, ReadOnlyMemory<byte> jsonUtf8);

    string? ExtractStringField(string field, ReadOnlyMemory<byte> jsonUtf8);
}
