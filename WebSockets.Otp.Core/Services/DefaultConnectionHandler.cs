using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Core.Logging;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Services;

public sealed class DefaultConnectionHandler(
    IWsConnectionManager connectionManager, IWsConnectionFactory connectionFactory, IHandshakeHandler hanshakeService,
    IContextFactory contextFactory, IMessageProcessorStore processorResolver, ISerializerStore serializerStore,
    ILogger<DefaultConnectionHandler> logger) : IConnectionHandler
{
    public async Task HandleAsync(HttpContext context, WsConfiguration config)
    {
        var traceId = new TraceId(context);

        logger.RequestProcessingStarted(traceId);

        var token = context.RequestAborted;

        using var socket = await context.WebSockets.AcceptWebSocketAsync();

        var handshakeOptions = await hanshakeService.HandleAsync(context, socket, config, token);
        if (handshakeOptions is null)
        {
            logger.HandshakeOptionsNotFound(traceId);
            return;
        }

        logger.HandshakeCompleted(handshakeOptions.Protocol, traceId);

        if (!serializerStore.TryGet(handshakeOptions.Protocol, out var serializer))
        {
            logger.SerializerNotFound(handshakeOptions.Protocol, traceId);
            return;
        }

        var connection = connectionFactory.Create(socket, serializer);

        if (!await connectionManager.TryAdd(connection, token))
        {
            logger.ConnectionAddFailed(connection.Id, traceId);
            return;
        }

        logger.ConnectionEstablished(connection.Id, traceId);

        var globalContext = contextFactory.CreateGlobal(context, socket, connection.Id, connectionManager);
        try
        {
            logger.InvokingOnConnectedCallback(connection.Id, traceId);
            config.OnConnected?.Invoke(globalContext);

            var messageProcessor = processorResolver.Get(config.ProcessingMode);

            logger.MessageProcessingStarted(connection.Id, traceId);

            await messageProcessor.Process(globalContext, serializer, config, token);

            logger.MessageProcessingCompleted(connection.Id, traceId);
        }
        finally
        {
            logger.RemovingConnection(connection.Id, traceId);
            await connectionManager.TryRemove(connection.Id, token);

            logger.InvokingOnDisconnectedCallback(connection.Id, traceId);
            config.OnDisconnected?.Invoke(globalContext);

            logger.ConnectionClosed(connection.Id, traceId);
        }
    }
}
