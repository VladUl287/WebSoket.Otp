using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core;

public sealed class WsConnectionFactory(IIdProvider idProvider) : IWsConnectionFactory
{
    public IWsConnection Create(HttpContext context, WebSocket socket)
    {
        var connectionId = idProvider.Create();
        return new WsConnection(connectionId, context, socket);
    }

    public ConnectionSettings CreateOptions(HttpContext context, WsMiddlewareOptions options)
    {
        return new ConnectionSettings
        {
            User = context.User
        };
    }

    public string GetConnectionToken(HttpContext context)
    {
        const string queryKey = "id";

        if (!context.Request.Query.TryGetValue(queryKey, out var idValues) || idValues.Count == 0)
            return string.Empty;

        return idValues.ToString();
    }
}
