using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Middlewares;

public sealed class WsMiddleware(
    RequestDelegate next, IHandshakeRequestProcessor handshakeProcessor, IWsRequestProcessor mainProcessor, 
    WsMiddlewareOptions options)
{
    public Task InvokeAsync(HttpContext context)
    {
        if (handshakeProcessor.IsHandshakeRequest(context, options))
            return handshakeProcessor.HandleRequestAsync(context, options);

        if (mainProcessor.IsWebSocketRequest(context, options))
            return mainProcessor.HandleRequestAsync(context, options);

        return next(context);
    }
}
