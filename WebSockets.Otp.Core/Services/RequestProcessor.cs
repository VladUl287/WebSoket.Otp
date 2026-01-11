using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Logging;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Services;

public sealed class RequestProcessor(
    IWsService wsService,
    IHandshakeRequestParser handshakeRequestParser,
    ISerializerResolver serializerResolver,
    IStateService requestState,
    ITokenIdService tokenIdService,
    ILogger<RequestProcessor> logger) : IWsRequestProcessor
{
    public async Task HandleRequestAsync(HttpContext ctx, WsMiddlewareOptions options)
    {
        await wsService.HandleRequestAsync(ctx, options);
    }

    public async Task HandleRequestAsync(ConnectionContext ctx, WsMiddlewareOptions options)
    {
        var handshakeOptions = await HandshakeAsync(ctx, default);

        if (handshakeOptions is null)
            return;

        await wsService.HandleRequestAsync(ctx, options);
    }

    internal async ValueTask<WsConnectionOptions?> HandshakeAsync(ConnectionContext ctx, CancellationToken token)
    {
        var connectionContext = ctx.Features.Get<IConnectionTransportFeature>()
            ?? throw new NullReferenceException();

        var input = connectionContext.Transport.Input;

        while (true)
        {
            var result = await input.ReadAsync(token);

            if (result.IsCanceled)
                return null;

            var buffer = result.Buffer;

            var consumed = buffer.Start;
            var examined = buffer.End;

            try
            {
                if (buffer.IsEmpty)
                    continue;

                if (result.IsCompleted)
                    return null;

                var segment = buffer;

                var options = await handshakeRequestParser.Parse(segment);

                consumed = segment.Start;
                examined = consumed;

                if (!serializerResolver.Contains(options.Protocol))
                {
                    return null;
                }

                //var successResult = Array.Empty<byte>().AsMemory();
                //await connectionContext.Transport.Output.WriteAsync(successResult, token);

                return options;
            }
            finally
            {
                input.AdvanceTo(consumed, examined);
            }
        }
    }
}
