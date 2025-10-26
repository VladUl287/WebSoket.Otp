using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.AspNet.Logging;

namespace WebSockets.Otp.AspNet.Middlewares;

public sealed class WsMiddleware(
    RequestDelegate next, IHandshakeRequestProcessor handshakeProcessor, IWsRequestProcessor wsProcessor,
    ILogger<WsMiddleware> logger, WsMiddlewareOptions options)
{
    public Task InvokeAsync(HttpContext context)
    {
        try
        {
            if (handshakeProcessor.IsHandshakeRequest(context, options))
                return handshakeProcessor.HandleRequestAsync(context, options);

            if (wsProcessor.IsWebSocketRequest(context, options))
                return wsProcessor.HandleWebSocketRequestAsync(context, options);

            return next(context);
        }
        catch (Exception ex)
        {
            logger.WsMiddlewareError(ex);
            throw;
        }
    }
}
