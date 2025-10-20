using System.Collections.Concurrent;
using System.Reflection;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Extensions;

namespace WebSockets.Otp.AspNet;

public sealed class EndpointInvoker(IMethodResolver methodResolver) : IEndpointInvoker
{
    private readonly ConcurrentDictionary<Type, Func<object, IWsExecutionContext, CancellationToken, Task>> _cache = new();

    public Task InvokeEndpointAsync(object endpointInstance, IWsExecutionContext ctx, CancellationToken ct)
    {
        var fn = GetInvoker(endpointInstance.GetType());
        return fn(endpointInstance, ctx, ct);
    }

    public Func<object, IWsExecutionContext, CancellationToken, Task> GetInvoker(Type endpointType)
    {
        ArgumentNullException.ThrowIfNull(endpointType, nameof(endpointType));
        return _cache.GetOrAdd(endpointType, CreateInvoker);
    }

    private Func<object, IWsExecutionContext, CancellationToken, Task> CreateInvoker(Type endpointType)
    {
        var handleMethod = methodResolver.ResolveHandleMethod(endpointType);

        if (endpointType.AcceptsRequestMessages())
            return CreateRequestHandlingInvoker(endpointType, handleMethod);

        return CreateConnectionHandlingInvoker(handleMethod);
    }

    private static Func<object, IWsExecutionContext, CancellationToken, Task> CreateRequestHandlingInvoker(Type endpointType, MethodInfo handleMethod)
    {
        return (endpointInst, execCtx, token) =>
        {
            var requestType = endpointType.GetRequestType();
            var requestData = execCtx.Serializer.Deserialize(requestType, execCtx.RawPayload);
            var invocation = handleMethod.Invoke(endpointInst, [requestData, execCtx, token])
                ?? throw new InvalidOperationException("HandleAsync method returned null");

            return (Task)invocation;
        };
    }

    private static Func<object, IWsExecutionContext, CancellationToken, Task> CreateConnectionHandlingInvoker(MethodInfo handleMethod)
    {
        return (endpointInst, execCtx, token) =>
        {
            var invocation = handleMethod.Invoke(endpointInst, [execCtx, token])
                ?? throw new InvalidOperationException("HandleAsync method returned null");

            return (Task)invocation;
        };
    }
}
