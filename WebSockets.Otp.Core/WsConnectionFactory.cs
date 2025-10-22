using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core;

public sealed class WsConnectionFactory(IIdProvider idProvider, IWsAuthorizationService authService) : IWsConnectionFactory
{
    public IWsConnection Create(HttpContext context, WebSocket socket)
    {
        var connectionId = idProvider.Create();
        return new WsConnection(connectionId, context, socket);
    }

    public WsConnectionOptions CreateOptions(HttpContext context, WsMiddlewareOptions options)
    {
        var connectionOptions = new WsConnectionOptions();

        if (options is { Authorization.RequireAuthorization: true })
        {
            var authResult = authService.AuhtorizeAsync(context, options.Authorization).GetAwaiter().GetResult();
            if (authResult.Failed)
                throw new Exception(authResult.FailureReason);

            connectionOptions.User = context.User;
        }

        return connectionOptions;
    }
}
