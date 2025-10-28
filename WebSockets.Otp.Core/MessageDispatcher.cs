using WebSockets.Otp.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Core.Exceptions;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core;

public class MessageDispatcher(
    IServiceScopeFactory scopeFactory, IWsEndpointRegistry endpointRegistry, IExecutionContextFactory contextFactory,
    IEndpointInvoker invoker, IStringPool stringPool, ILogger<MessageDispatcher> logger) : IMessageDispatcher
{
    private readonly ReadOnlyMemory<byte> KeyField = stringPool.Encoding.GetBytes(nameof(WsMessage.Key).ToLowerInvariant()).AsMemory();

    public async Task DispatchMessage(IWsConnection connection, ISerializer serializer, IMessageBuffer buffer, CancellationToken token)
    {
        var connectionId = connection.Id;

        var payload = buffer.Span;

        logger.LogDispatchingMessage(connectionId, "Unknown", payload.Length);

        var endpointKey = serializer.ExtractStringField(KeyField.Span, payload, stringPool);

        if (endpointKey is null)
        {
            logger.LogKeyExtractionFailed(connectionId);
            throw new MessageFormatException("Unable to determine message route from payload");
        }

        var endpointType = endpointRegistry.Resolve(endpointKey);
        if (endpointType is null)
        {
            logger.LogEndpointNotFound(connectionId, endpointKey);
            throw new EndpointNotFoundException($"Endpoint for route '{endpointKey}' not found");
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        var endpointInstance = scope.ServiceProvider.GetService(endpointType);
        if (endpointInstance is null)
        {
            logger.LogEndpointServiceNotFound(connectionId, endpointType.Name);
            throw new EndpointNotFoundException($"Endoind with type '{endpointType}' not found");
        }

        logger.LogEndpointResolved(connectionId, endpointType.Name);

        var execCtx = contextFactory.Create(endpointKey, endpointType, connection, buffer, serializer, token);

        await invoker.InvokeEndpointAsync(endpointInstance, execCtx, token);

        logger.LogMessageDispatched(connectionId, endpointKey);
    }
}
