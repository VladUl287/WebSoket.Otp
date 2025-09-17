using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Abstractions;
using System.Text;
using System.Text.Json;
using WebSockets.Otp.Core.Extensions;

namespace WebSockets.Otp.Core;

public class MessageDispatcher(
    IServiceProvider root, IWsEndpointRegistry registry, IMessageSerializer serializer,
    EndpointInvoker invoker) : IMessageDispatcher
{
    public async Task DispatchMessage(IWsContext publicCtx, ReadOnlyMemory<byte> payload, CancellationToken token = default)
    {
        string route;
        try
        {
            route = serializer.PeekRoute(payload);
        }
        catch
        {
            var b = serializer.Serialize(new InternalError("unknown", "invalid_payload"));
            await publicCtx.Connection.SendAsync(b, WebSocketMessageType.Text, publicCtx.Cancellation);
            return;
        }

        var desc = registry.Get(route);
        if (desc == null)
        {
            var b = serializer.Serialize(new InternalError(route, "no_endpoint"));
            await publicCtx.Connection.SendAsync(b, WebSocketMessageType.Text, publicCtx.Cancellation);
            return;
        }

        await using var scope = root.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var execCtx = new WsExecutionContext(sp, publicCtx.Connection, payload, route, serializer, publicCtx.Cancellation)
        {
            Endpoint = desc
        };

        var requestPresented = desc.BaseType.IsGenericType && desc.BaseType.GetGenericTypeDefinition() == typeof(WsEndpoint<>);
        if (requestPresented)
        {
            try
            {
                var reqType = desc.BaseType.GetGenericArguments()[0];
                if (reqType.ImplementsInterface<IWsMessage>())
                {
                    var req = serializer.Deserialize(reqType, payload);
                    execCtx.RequestMessage = req;
                }
            }
            catch (Exception ex)
            {
                var b = serializer.Serialize(new InternalError(route, "invalid_payload"));
                await publicCtx.Connection.SendAsync(b, WebSocketMessageType.Text, publicCtx.Cancellation);
                return;
            }
        }

        var behaviorInstances = new List<object>();

        var endpointInstance = sp.GetService(desc) ?? ActivatorUtilities.CreateInstance(sp, desc);

        await invoker.InvokeEndpointAsync(endpointInstance, execCtx, token);

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //Task terminal(EndpointInvoker endpointInvoker) => invoker.InvokeEndpointAsync(endpointInstance, execCtx, token);

        //var pipeline = behaviorInstances.Aggregate(terminal, (next, behavior) => () => behavior.InvokeAsync(execCtx, next));

        //await pipeline();
    }

    private record InternalError(string Route, string Error) : IWsMessage;
}
