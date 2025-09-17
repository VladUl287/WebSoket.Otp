using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core;

public sealed class EndpointInvoker
{
    private readonly ConcurrentDictionary<Type, Func<object, WsExecutionContext, CancellationToken, Task>> _cache = new();

    public Func<object, WsExecutionContext, CancellationToken, Task> GetInvoker(Type endpointType)
    {
        return _cache.GetOrAdd(endpointType, t =>
        {
            // two cases:
            // - inherits WsEndpoint (raw) -> Task HandleAsync(IWsConnection, CancellationToken)
            // - inherits WebSocketEndpoint<TReq> -> Task HandleAsync(TReq, IWsContext, CancellationToken)
            var bt = t;
            while (bt != null)
            {
                if (bt.IsGenericType && bt.GetGenericTypeDefinition() == typeof(WsEndpoint<>))
                {
                    var reqType = bt.GenericTypeArguments[0];

                    // build invoked delegate using reflection
                    var handleMethod = t.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { reqType, 
                        typeof(IWsContext), typeof(CancellationToken) }, null)
                                         ?? throw new InvalidOperationException($"HandleAsync({reqType.Name}, IWsContext, CancellationToken) not found on {t.Name}");

                    return (endpointInst, execCtx, token) =>
                    {
                        // request object must be of reqType
                        var reqObj = execCtx.RequestMessage;
                        if (reqObj == null || !reqType.IsInstanceOfType(reqObj))
                        {
                            // attempt to convert
                            var txt = Encoding.UTF8.GetString(execCtx.RawPayload.Span);
                            reqObj = JsonSerializer.Deserialize(txt, reqType, execCtx.RequestServices
                                .GetService<JsonSerializerOptions>() ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }

                        var task = (Task)handleMethod.Invoke(endpointInst, [reqObj, execCtx.AsPublicContext(), token])!;
                        return task;
                    };
                }

                bt = bt.BaseType;
            }

            // raw WsEndpoint
            var rawHandle = t.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(IWsConnection), typeof(CancellationToken) }, null)
                        ?? throw new InvalidOperationException($"HandleAsync(IWsConnection, CancellationToken) not found on {t.Name}");

            return (endpointInst, execCtx, token) =>
            {
                var task = (Task)rawHandle.Invoke(endpointInst, new object[] { execCtx.Connection, token })!;
                return task;
            };
        });
    }

    public Task InvokeEndpointAsync(object endpointInstance, WsExecutionContext ctx, CancellationToken ct)
    {
        var fn = GetInvoker(endpointInstance.GetType());
        return fn(endpointInstance, ctx, ct);
    }
}
