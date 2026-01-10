using System.Diagnostics.CodeAnalysis;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface ISerializerResolver
{
    bool Contains(string protocol);

    bool TryResolve(string protocol, [NotNullWhen(true)] out ISerializer? serializer);
}
