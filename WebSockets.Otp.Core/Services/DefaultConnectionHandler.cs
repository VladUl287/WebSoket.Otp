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
    IContextFactory contextFactory, IMessageProcessor messageProcessor, ISerializerStore serializerStore,
    ILogger<DefaultConnectionHandler> logger) : IConnectionHandler
{
    public async Task HandleAsync(HttpContext context, WsOptionsSnapshot options)
    {
        var requestId = new RequestId(context);

        logger.RequestProcessingStarted(requestId);

        var token = context.RequestAborted;

        using var socket = await context.WebSockets.AcceptWebSocketAsync();

        var handshakeOptions = await hanshakeService.HandleAsync(context, socket, options, token);
        if (handshakeOptions is null)
        {
            logger.HandshakeOptionsNotFound(requestId);
            return;
        }

        logger.HandshakeCompleted(handshakeOptions.Protocol, requestId);

        if (!serializerStore.TryGet(handshakeOptions.Protocol, out var serializer))
        {
            logger.SerializerNotFound(handshakeOptions.Protocol, requestId);
            return;
        }

        var connection = connectionFactory.Create(socket, serializer);

        if (!await connectionManager.TryAdd(connection, token))
        {
            logger.ConnectionAddFailed(connection.Id, requestId);
            return;
        }

        logger.ConnectionEstablished(connection.Id, requestId);

        var globalContext = contextFactory.CreateGlobal(context, socket, connection.Id, options);
        try
        {
            logger.InvokingOnConnectedCallback(connection.Id, requestId);
            options.OnConnected?.Invoke(globalContext);

            logger.MessageProcessingStarted(connection.Id, requestId);

            await messageProcessor.Process(globalContext, serializer, token);

            logger.MessageProcessingCompleted(connection.Id, requestId);
        }
        finally
        {
            logger.RemovingConnection(connection.Id, requestId);
            await connectionManager.TryRemove(connection.Id, token);

            logger.InvokingOnDisconnectedCallback(connection.Id, requestId);
            options.OnDisconnected?.Invoke(globalContext);

            logger.ConnectionClosed(connection.Id, requestId);
        }
    }
}
