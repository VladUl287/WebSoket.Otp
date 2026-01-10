using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IMessageProcessor
{
    string Name { get; }

    Task Process(IWsConnection connection, WsMiddlewareOptions options, WsConnectionOptions connectionOptions);
}
