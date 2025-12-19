using System.Diagnostics.CodeAnalysis;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface ISerializerResolver
{
    bool Registered(string format);

    bool TryResolve(string format, [NotNullWhen(true)] out ISerializer? serializer);
}
