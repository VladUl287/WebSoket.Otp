using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Abstractions.Serializers;

public interface ISerializer
{
    string Format { get; }

    ReadOnlyMemory<byte> Serialize<T>(T message);

    object? Deserialize(Type type, ReadOnlySpan<byte> data);

    T? Deserialize<T>(ReadOnlySpan<byte> data);

    string ExtractField(ReadOnlySpan<byte> field, ReadOnlySpan<byte> data);

    string ExtractField(ReadOnlySpan<byte> field, ReadOnlySpan<byte> data, IStringPool stringPool);
}
