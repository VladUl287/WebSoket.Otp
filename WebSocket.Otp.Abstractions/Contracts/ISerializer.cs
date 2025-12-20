namespace WebSockets.Otp.Abstractions.Contracts;

public interface ISerializer
{
    string Format { get; }

    ReadOnlyMemory<byte> Serialize<T>(T message);

    object? Deserialize(Type type, ReadOnlyMemory<byte> data);
    object? Deserialize(Type type, ReadOnlySpan<byte> data);

    string? ExtractStringField(string field, ReadOnlySpan<byte> data);
    string? ExtractStringField(ReadOnlySpan<byte> field, ReadOnlySpan<byte> data);
    string? ExtractStringField(string field, ReadOnlySpan<byte> data, IStringPool stringPool);
    string? ExtractStringField(ReadOnlySpan<byte> field, ReadOnlySpan<byte> data, IStringPool stringPool);
}
