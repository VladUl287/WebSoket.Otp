namespace WebSockets.Otp.Abstractions.Contracts;

public interface ISerializer
{
    string Format { get; }

    ReadOnlyMemory<byte> Serialize<T>(T message);

    object? Deserialize(Type type, ReadOnlyMemory<byte> jsonUtf8);
    object? Deserialize(Type type, ReadOnlySpan<byte> jsonUtf8);

    string? ExtractStringField(string field, ReadOnlyMemory<byte> jsonUtf8);
    string? ExtractStringField(string field, ReadOnlySpan<byte> jsonUtf8, IStringPool stringIntern);
    string? ExtractStringField(string field, ReadOnlyMemory<byte> jsonUtf8, IStringPool stringIntern);
}
