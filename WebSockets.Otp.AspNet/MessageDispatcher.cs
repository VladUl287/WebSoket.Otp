using WebSockets.Otp.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Core;
using WebSockets.Otp.AspNet.Extensions;

namespace WebSockets.Otp.AspNet;

public class MessageDispatcher(
    IServiceScopeFactory root, IWsEndpointRegistry endpointRegistry, IMessageSerializer serializer,
    EndpointInvoker invoker) : IMessageDispatcher
{
    public sealed class MessageFormatException(string message) : FormatException(message)
    { }

    public sealed class EndpointNotFoundException(string message) : InvalidOperationException(message)
    { }

    public sealed class MessageSerializationException(string message, Exception? inner = null) : InvalidOperationException(message, inner)
    { }

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

        //var behaviorInstances = new List<Behaviour>();

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //Task terminal() => invoker.InvokeEndpointAsync(endpointInstance, execCtx, token);

        //Behaviour firstStep = async (next) =>
        //{
        //    Debug.WriteLine("Pipeline first step");
        //    await next();
        //};
        //behaviorInstances.Add(firstStep);

        //behaviorInstances.Reverse();
        //var pipeline = behaviorInstances.Aggregate(terminal, (next, behavior) =>
        //{
        //    return () => behavior(next);
        //});

        //await pipeline();
    }

    //private delegate Task Behaviour(Func<Task> next);
}
