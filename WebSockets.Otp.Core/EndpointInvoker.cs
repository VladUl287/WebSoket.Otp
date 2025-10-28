using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Logging;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Abstractions;

namespace WebSockets.Otp.Core;

public sealed class EndpointInvoker(IMethodResolver methodResolver, ILogger<EndpointInvoker> _logger) : IEndpointInvoker
{
    private readonly ConcurrentDictionary<Type, Func<object, IWsExecutionContext, CancellationToken, Task>> _cache = new();

    public async Task InvokeEndpointAsync(object endpointInstance, IWsExecutionContext ctx, CancellationToken ct)
    {
        var endpointType = endpointInstance.GetType();
        var connectionId = ctx.Connection.Id;

        try
        {
            _logger.LogInvokingEndpoint(endpointType.Name, connectionId);

            var invoker = GetOrAddInvoker(endpointType);
            await invoker(endpointInstance, ctx, ct);

            _logger.LogInvocationSuccess(endpointType.Name, invoker.Method.Name);
        }
        catch (Exception ex)
        {
            _logger.LogInvocationFailed(ex, endpointType.Name, connectionId);
            throw;
        }
    }

    public Func<object, IWsExecutionContext, CancellationToken, Task> GetOrAddInvoker(Type endpointType)
    {
        if (_cache.TryGetValue(endpointType, out var cachedInvoker))
        {
            _logger.LogCacheHit(endpointType.Name);
            return cachedInvoker;
        }

        _logger.LogCacheMiss(endpointType.Name);

        var invoker = CreateInvoker(endpointType);
        _cache[endpointType] = invoker;
        return invoker;
    }

    private Func<object, IWsExecutionContext, CancellationToken, Task> CreateInvoker(Type endpointType)
    {
        _logger.LogCreatingInvoker(endpointType.Name);

        var handleMethod = methodResolver.ResolveHandleMethodFromBase(endpointType);
        if (handleMethod is null)
        {
            _logger.LogNullMethodResolution(endpointType.Name);
            throw new InvalidOperationException($"Could not resolve handle method for endpoint type {endpointType.Name}");
        }

        try
        {
            if (endpointType.AcceptsRequestMessages())
            {
                return CreateRequestHandlingInvoker(endpointType, handleMethod);
            }

            return CreateConnectionHandlingInvoker(handleMethod);
        }
        catch (Exception ex)
        {
            _logger.LogMethodResolutionFailed(endpointType.Name);
            throw new InvalidOperationException($"Failed to create invoker for endpoint type {endpointType.Name}", ex);
        }
    }

    private static Func<object, IWsExecutionContext, CancellationToken, Task> CreateRequestHandlingInvoker(Type endpointType, MethodInfo handleMethod)
    {
        return (endpointInst, execCtx, token) =>
        {
            var requestType = endpointType.GetRequestType();
            var requestData = execCtx.Serializer.Deserialize(requestType, execCtx.RawPayload.Span);
            var invocation = handleMethod.Invoke(endpointInst, [requestData, execCtx, token])
                ?? throw new InvalidOperationException("HandleAsync method returned null");

            return (Task)invocation;
        };
    }

    private static Func<object, IWsExecutionContext, CancellationToken, Task> CreateConnectionHandlingInvoker(MethodInfo handleMethod)
    {
        var func = handleMethod.CreateDelegate<Func<WsEndpoint, IWsExecutionContext, CancellationToken, Task>>();
        return (endpointInst, execCtx, token) =>
        {
            if (endpointInst is not WsEndpoint endpoint)
                throw new Exception("Cannot call handler for non WsEndpoint derived type");

            return func(endpoint, execCtx, token);
        };
    }
}
