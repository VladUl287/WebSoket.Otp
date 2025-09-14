namespace WebSockets.Otp.Abstractions.Contracts;

public interface IMessageSerializer
{
    ReadOnlyMemory<byte> Serialize<T>(T message) where T : IMessage;
    T? Deserialize<T>(ReadOnlyMemory<byte> payload) where T : class, IMessage;
    string PeekRoute(ReadOnlyMemory<byte> payload);
}
