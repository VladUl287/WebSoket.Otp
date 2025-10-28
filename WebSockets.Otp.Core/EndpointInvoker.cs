using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Logging;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Abstractions;
using System.Linq.Expressions;

namespace WebSockets.Otp.Core;

public sealed class EndpointInvoker(IMethodResolver methodResolver, ILogger<EndpointInvoker> logger) : IEndpointInvoker
{
    private readonly ConcurrentDictionary<Type, Func<object, IWsExecutionContext, CancellationToken, Task>> _cache = new();

    public async Task InvokeEndpointAsync(object endpointInstance, IWsExecutionContext ctx, CancellationToken ct)
    {
        var endpointType = endpointInstance.GetType();
        var connectionId = ctx.Connection.Id;

        try
        {
            logger.LogInvokingEndpoint(endpointType.Name, connectionId);

            var invoker = GetOrAddInvoker(endpointType);
            await invoker(endpointInstance, ctx, ct);

            logger.LogInvocationSuccess(endpointType.Name, connectionId);
        }
        catch (Exception ex)
        {
            logger.LogInvocationFailed(ex, endpointType.Name, connectionId);
            throw;
        }
    }

    public Func<object, IWsExecutionContext, CancellationToken, Task> GetOrAddInvoker(Type endpointType)
    {
        if (_cache.TryGetValue(endpointType, out var cachedInvoker))
        {
            logger.LogCacheHit(endpointType.Name);
            return cachedInvoker;
        }

        logger.LogCacheMiss(endpointType.Name);

        var invoker = CreateInvoker(endpointType);
        _cache[endpointType] = invoker;
        return invoker;
    }

    private Func<object, IWsExecutionContext, CancellationToken, Task> CreateInvoker(Type endpointType)
    {
        logger.LogCreatingInvoker(endpointType.Name);

        var baseEndpType = endpointType.GetBaseEndpointType();
        var handleMethod = methodResolver.ResolveHandleMethod(baseEndpType);
        if (handleMethod is null)
        {
            logger.LogNullMethodResolution(endpointType.Name);
            throw new InvalidOperationException($"Could not resolve handle method for endpoint type {endpointType.Name}");
        }

        try
        {
            if (baseEndpType.AcceptsRequestMessages())
            {
                return CreateRequestHandlingInvoker(baseEndpType, handleMethod);
            }

            return CreateConnectionHandlingInvoker(handleMethod);
        }
        catch (Exception ex)
        {
            logger.LogMethodResolutionFailed(endpointType.Name);
            throw new InvalidOperationException($"Failed to create invoker for endpoint type {endpointType.Name}", ex);
        }
    }

    private static Func<object, IWsExecutionContext, CancellationToken, Task> CreateRequestHandlingInvoker(Type baseEndpType, MethodInfo handleMethod)
    {
        var handler = CreateHandlerDelegateExpr(baseEndpType, handleMethod);
        return (endpointInst, execCtx, token) =>
        {
            var requestType = baseEndpType.GetRequestType();
            var requestData = execCtx.Serializer.Deserialize(requestType, execCtx.RawPayload.Span) as IWsMessage ??
                throw new NullReferenceException();

            return handler(endpointInst, requestData, execCtx, token);
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

    private static Func<object, IWsMessage, IWsExecutionContext, CancellationToken, Task> CreateHandlerDelegateExpr(Type baseEndpType, MethodInfo handleMethod)
    {
        var baseType = baseEndpType.GetBaseEndpointType();
        var messageType = baseEndpType.GetRequestType();

        var instanceParam = Expression.Parameter(typeof(object));
        var messageParam = Expression.Parameter(typeof(IWsMessage));
        var contextParam = Expression.Parameter(typeof(IWsExecutionContext));
        var cancellationParam = Expression.Parameter(typeof(CancellationToken));

        var typedInstance = Expression.Convert(instanceParam, baseType);
        var typedMessage = Expression.Convert(messageParam, messageType);

        var callExpression = Expression.Call(typedInstance, handleMethod, typedMessage, contextParam, cancellationParam);

        var lambda = Expression.Lambda<Func<object, IWsMessage, IWsExecutionContext, CancellationToken, Task>>(
            callExpression, instanceParam, messageParam, contextParam, cancellationParam);

        return lambda.Compile();
    }
}
