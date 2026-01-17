using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions.Transport;

public interface INewMessageProcessor
{
    string Name { get; }

    Task Process(
        ConnectionContext context, IGlobalContext globalContext, WsMiddlewareOptions options,
        WsConnectionOptions connectionOptions, CancellationToken token);
}
