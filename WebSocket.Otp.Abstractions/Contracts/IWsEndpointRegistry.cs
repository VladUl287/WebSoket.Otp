namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsEndpointRegistry
{
    Type? Resolve(string path);

    IEnumerable<Type> Enumerate();

    void Register(Type type);

    void Register(IEnumerable<Type> types);
}
