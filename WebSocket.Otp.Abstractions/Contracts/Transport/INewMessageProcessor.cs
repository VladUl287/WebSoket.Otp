using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts.Transport;

public interface INewMessageProcessor
{
    string Name { get; }

    Task Process(ConnectionContext context, IWsConnection connection, WsMiddlewareOptions options, 
        WsConnectionOptions connectionOptions, CancellationToken cancellationToken);
}
