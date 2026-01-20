using System.Diagnostics.CodeAnalysis;

namespace WebSockets.Otp.Abstractions.Serializers;

public interface ISerializerStore
{
    bool TryGet(string protocol, [NotNullWhen(true)] out ISerializer? serializer);
}
