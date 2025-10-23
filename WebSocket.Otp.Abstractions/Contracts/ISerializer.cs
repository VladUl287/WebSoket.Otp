namespace WebSockets.Otp.Abstractions.Contracts;

public interface ISerializer
{
    string Format { get; }

    ReadOnlyMemory<byte> Serialize<T>(T message);

    object? Deserialize(Type type, ReadOnlyMemory<byte> jsonUtf8);

    string? ExtractStringField(string field, ReadOnlyMemory<byte> jsonUtf8);
}
