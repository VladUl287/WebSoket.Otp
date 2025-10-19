using WebSockets.Otp.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Core;
using WebSockets.Otp.Core.Exceptions;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Abstractions;

namespace WebSockets.Otp.AspNet;

public class MessageDispatcher(
    IServiceScopeFactory scopeFactory, IWsEndpointRegistry endpointRegistry, IMessageSerializer serializer, 
    EndpointInvoker invoker) : IMessageDispatcher
{
    private static readonly string KeyField = nameof(WsMessage.Key).ToLowerInvariant();

    public async Task DispatchMessage(IWsConnection connection, ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        var endpointKey = string.Intern(serializer.ExtractStringField(KeyField, payload) ??
            throw new MessageFormatException("Unable to determine message route from payload"));

        var endpointType = endpointRegistry.Resolve(endpointKey) ??
            throw new EndpointNotFoundException($"Endpoint for route '{endpointKey}' not found");

        await using var scope = scopeFactory.CreateAsyncScope();

        var serviceProvider = scope.ServiceProvider;
        var endpointInstance = serviceProvider.GetService(endpointType) ??
            throw new EndpointNotFoundException($"Endoind with type '{endpointType}' not found");

        var execCtx = new WsExecutionContext(endpointKey, serviceProvider, connection, payload, serializer, token)
        {
            Endpoint = endpointType
        };

        if (endpointType.AcceptsRequestMessages())
        {
            var requestType = endpointType.GetRequestType();
            var reqestData = serializer.Deserialize(requestType, payload);
            execCtx.RequestMessage = reqestData;
        }

        await invoker.InvokeEndpointAsync(endpointInstance, execCtx, token);
    }

    public Task DispatchMessage(IWsConnection connection, IMessageBuffer payload, CancellationToken token)
    {
        var endpointKey = string.Intern(serializer.ExtractStringField(KeyField, payload.Span) ??
            throw new MessageFormatException("Unable to determine message route from payload"));

        throw new NotImplementedException();
    }
}
