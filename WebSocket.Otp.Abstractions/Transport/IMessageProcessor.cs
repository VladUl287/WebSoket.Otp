using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageProcessor
{
    string Mode { get; }

    Task Process(
        ConnectionContext context, IGlobalContext globalContext, WsMiddlewareOptions options,
        WsConnectionOptions connectionOptions, CancellationToken token);
}
