using System.Collections.Concurrent;
using System.Reflection;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core;
using WebSockets.Otp.Core.Extensions;

namespace WebSockets.Otp.AspNet;

public sealed class EndpointInvoker
{
    private readonly ConcurrentDictionary<Type, Func<object, WsExecutionContext, CancellationToken, Task>> _cache = new();

    public Func<object, WsExecutionContext, CancellationToken, Task> GetInvoker(Type endpointType)
    {
        return _cache.GetOrAdd(endpointType, (type) =>
        {
            if (type.AcceptsRequestMessages())
            {
                var baseType = type.GetBaseEndpointType();
                var reqType = baseType.GenericTypeArguments[0];

                var handleMethod = type.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic, binder: null,
                    [reqType, typeof(IWsContext), typeof(CancellationToken)], null) ??
                    throw new InvalidOperationException($"HandleAsync({reqType.Name}, IWsContext, CancellationToken) not found on {type.Name}");

                return (endpointInst, execCtx, token) =>
                {
                    var reqObj = execCtx.RequestMessage;
                    var invocation = handleMethod.Invoke(endpointInst, [reqObj, execCtx, token]) ??
                        throw new NullReferenceException();
                    return (Task)invocation;
                };
            }

            var rawHandle = type.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic, null,
                [typeof(IWsConnection), typeof(CancellationToken)], null) ??
                throw new InvalidOperationException($"HandleAsync(IWsConnection, CancellationToken) not found on {type.Name}");

            return (endpointInst, execCtx, token) =>
            {
                var invocation = rawHandle.Invoke(endpointInst, [execCtx.Connection, token]) ??
                    throw new NullReferenceException();
                return (Task)invocation;
            };
        });
    }

    public Task InvokeEndpointAsync(object endpointInstance, WsExecutionContext ctx, CancellationToken ct)
    {
        var fn = GetInvoker(endpointInstance.GetType());
        return fn(endpointInstance, ctx, ct);
    }
}
