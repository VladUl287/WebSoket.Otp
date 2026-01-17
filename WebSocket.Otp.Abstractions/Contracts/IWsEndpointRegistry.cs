using System.Diagnostics.CodeAnalysis;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsEndpointRegistry
{
    bool TryResolve(string path, [NotNullWhen(true)] out Type? endpoint);

    IEnumerable<Type> Enumerate();

    void Register(IEnumerable<Type> types);
}
