using WebSockets.Otp.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Core.Exceptions;
using WebSockets.Otp.Core.Extensions;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.AspNet.Logging;

namespace WebSockets.Otp.AspNet;

public class MessageDispatcher(
    IServiceScopeFactory scopeFactory, IWsEndpointRegistry endpointRegistry, IExecutionContextFactory contextFactory, 
    IEndpointInvoker invoker, IStringIntern keyStringIntern, ILogger<MessageDispatcher> logger) : IMessageDispatcher
{
    private static readonly string KeyField = nameof(WsMessage.Key).ToLowerInvariant();

    public async Task DispatchMessage(IWsConnection connection, ISerializer serializer, ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        var connectionId = connection.Id;

        logger.LogDispatchingMessage(connectionId, "Unknown", payload.Length);

        var endpointKey = serializer.ExtractStringField(KeyField, payload, keyStringIntern);
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

        logger.LogEndpointResolved(connectionId, endpointType.Name, endpointType.AcceptsRequestMessages());

        var execCtx = contextFactory.Create(endpointKey, endpointType, connection, payload, serializer, token);

        await invoker.InvokeEndpointAsync(endpointInstance, execCtx, token);

        logger.LogMessageDispatched(connectionId, endpointKey);
    }
}
