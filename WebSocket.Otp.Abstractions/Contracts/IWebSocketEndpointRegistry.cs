namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWebSocketEndpointRegistry
{
    void RegisterEndpoint<TEndpoint>(string pattern) where TEndpoint : Endpoint;

    void RegisterEndpoint<TEndpoint, TRequest>(string pattern)
        where TRequest : class
        where TEndpoint : Endpoint<TRequest>;
}
