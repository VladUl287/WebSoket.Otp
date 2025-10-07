using WebSockets.Otp.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Core;
using WebSockets.Otp.AspNet.Extensions;
using WebSockets.Otp.Core.Exceptions;

namespace WebSockets.Otp.AspNet;

public class MessageDispatcher(
    IServiceScopeFactory root, IWsEndpointRegistry endpointRegistry, IMessageSerializer serializer,
    EndpointInvoker invoker) : IMessageDispatcher
{
    public async Task DispatchMessage(IWsConnection connection, ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        var route = serializer.PeekRoute(payload) ??
            throw new MessageFormatException("Unable to determine message route from payload");

        var endpointType = endpointRegistry.Resolve(route) ??
            throw new EndpointNotFoundException($"Endpoint for route '{route}' not found");

        await using var scope = root.CreateAsyncScope();

        var serviceProvider = scope.ServiceProvider;
        var execCtx = new WsExecutionContext(serviceProvider, connection, payload, route, serializer, token)
        {
            Endpoint = endpointType
        };

        if (endpointType.AcceptsRequestMessages())
        {
            try
            {
                var requestType = endpointType.GetRequestType();
                var reqestData = serializer.Deserialize(requestType, payload);
                execCtx.RequestMessage = reqestData;
            }
            catch (Exception ex)
            {
                throw new MessageSerializationException("Invalid message format", ex);
            }
        }

        var endpointInstance = serviceProvider.GetService(endpointType) ??
            throw new EndpointNotFoundException($"Endoind with type '{endpointType}' not found");

        await invoker.InvokeEndpointAsync(endpointInstance, execCtx, token);
    }
}
