namespace WebSockets.Otp.Abstractions.Contracts;

public interface IMessageSerializer
{
    ReadOnlyMemory<byte> Serialize<T>(T message) where T : IWsMessage;

    T? Deserialize<T>(ReadOnlyMemory<byte> payload) where T : class, IWsMessage;

    object Deserialize(Type type, ReadOnlyMemory<byte> payload);

    string? ExtractStringField(string field, ReadOnlyMemory<byte> payload);
}
