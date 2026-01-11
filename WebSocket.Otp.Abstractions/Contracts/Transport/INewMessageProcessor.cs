using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts.Transport;

public interface INewMessageProcessor
{
    string Name { get; }

    Task Process(IAsyncEnumerable<IMessageBuffer> messages, IWsConnection connection, 
        WsMiddlewareOptions options, WsConnectionOptions connectionOptions);
}
