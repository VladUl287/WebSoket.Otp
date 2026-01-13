using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsRequestHandler
{
    Task HandleRequestAsync(ConnectionContext context, WsMiddlewareOptions options);
}
