using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Core.Logging;
using WebSockets.Otp.Abstractions.Configuration;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services;

public sealed partial class DefaultRequestHandler(
    IWsConnectionManager connectionManager, IWsConnectionFactory connectionFactory, IHandshakeService hanshakeService,
    IContextFactory contextFactory, IMessageProcessorStore processorResolver, ISerializerStore serializerStore,
    ILogger<DefaultRequestHandler> logger) : IRequestHandler
{
    public async Task HandleRequestAsync(HttpContext context, WsBaseConfiguration options)
    {
        logger.RequestProcessingStarted();

        var token = context.RequestAborted;

        using var socket = await context.WebSockets.AcceptWebSocketAsync();

        var handshakeOptions = await hanshakeService.ReceiveHandshakeOptions(context, socket, token);
        if (handshakeOptions is null)
        {
            logger.HandshakeOptionsNotFound();
            return;
        }

        logger.HandshakeCompleted(handshakeOptions.Protocol);

        if (!serializerStore.TryGet(handshakeOptions.Protocol, out var serializer))
        {
            logger.SerializerNotFound(handshakeOptions.Protocol);
            return;
        }

        var connection = connectionFactory.Create(socket, serializer);

        if (!connectionManager.TryAdd(connection))
        {
            logger.ConnectionAddFailed(connection.Id);
            return;
        }

        logger.ConnectionEstablished(connection.Id);

        var globalContext = contextFactory.CreateGlobal(context, socket, connection.Id, connectionManager);
        try
        {
            logger.InvokingOnConnectedCallback(connection.Id);
            options.OnConnected?.Invoke(globalContext);

            var messageProcessor = processorResolver.Get(options.ProcessingMode);

            logger.MessageProcessingStarted(connection.Id, options.ProcessingMode);

            await messageProcessor.Process(globalContext, serializer, options, token);

            logger.MessageProcessingCompleted(connection.Id);
        }
        finally
        {
            logger.RemovingConnection(connection.Id);
            connectionManager.TryRemove(connection.Id);

            logger.InvokingOnDisconnectedCallback(connection.Id);
            options.OnDisconnected?.Invoke(globalContext);

            logger.ConnectionClosed(connection.Id);
        }
    }
}
