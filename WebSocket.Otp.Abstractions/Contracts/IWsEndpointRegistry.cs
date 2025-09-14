namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsEndpointRegistry
{
    Type? Get(string path);

    IEnumerable<Type> GetAll();

    void Register(Type type);
}
