using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Logging;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Services;

public sealed class HandshakeRequestProcessor(
    IHandshakeRequestParser handshakeRequestParser,
    IStateService stateService,
    ITokenIdService tokenIdService,
    ISerializerResolver serializerResolver,
    ILogger<HandshakeRequestProcessor> logger) : IHandshakeRequestProcessor
{
    public async Task HandleRequestAsync(HttpContext context, WsMiddlewareOptions options)
    {
        var cancellationToken = context.RequestAborted;
        var traceId = new TraceId(context);

        logger.HandshakeRequestStarted(traceId);

        var connectionOptions = await handshakeRequestParser.Parse(new System.Buffers.ReadOnlySequence<byte>([]));
        if (connectionOptions is null)
        {
            logger.HandshakeBodyParseFail(traceId);
            await context.WriteAsync(StatusCodes.Status400BadRequest, "Unable to parse handshake request body", cancellationToken);
            return;
        }

        if (!serializerResolver.Contains(connectionOptions.Protocol))
        {
            logger.HandshakedUnsupportedProtocol(traceId, connectionOptions.Protocol);
            await context.WriteAsync(StatusCodes.Status400BadRequest, "Protocol not supported", cancellationToken);
            return;
        }

        var stateId = tokenIdService.Generate();

        await stateService.Set(stateId, connectionOptions, cancellationToken);

        await context.WriteAsync(StatusCodes.Status200OK, stateId, cancellationToken);

        logger.HandshakeRequestCompleted(context);
    }
}
