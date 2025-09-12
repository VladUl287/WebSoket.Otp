namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWebSocketEndpointRegistry
{
    void Register<TEndpoint>(string path) where TEndpoint : IEndpoint;

    IEndpoint Get(string path);
}
