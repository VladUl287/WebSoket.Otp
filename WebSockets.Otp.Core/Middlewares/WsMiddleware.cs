using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Middlewares;

public sealed class WsMiddleware(
    RequestDelegate next, IHandshakeRequestProcessor handshakeProcessor, IWsRequestProcessor wsProcessor, 
    WsMiddlewareOptions options)
{
    public Task InvokeAsync(HttpContext context)
    {
        if (handshakeProcessor.IsHandshakeRequest(context, options))
            return handshakeProcessor.HandleRequestAsync(context, options);

        if (wsProcessor.IsWebSocketRequest(context, options))
            return wsProcessor.HandleRequestAsync(context, options);

        return next(context);
    }
}
