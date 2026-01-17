using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Core.Extensions;

namespace WebSockets.Otp.Core.Services;

public sealed class EndpointInvoker : IEndpointInvoker
{
    private readonly ConcurrentDictionary<Type, Func<object, object, Task>> _cache = new();

    public Task Invoke(object endpoint, IEndpointContext context)
    {
        var endpointType = endpoint.GetType();
        var invokeDelegate = _cache.GetOrAdd(
            endpointType,
            (key, type) => CreateHandleDelegate(type),
            endpointType);
        return invokeDelegate(endpoint, context);
    }

    public Func<object, object, Task> CreateHandleDelegate(Type endpointType)
    {
        var baseEndpointType = endpointType.GetBaseEndpointType()!;
        var requestType = baseEndpointType.GetRequestType()!;

        var handleMethod = baseEndpointType.GetMethod(
            nameof(WsEndpoint.HandleAsync), BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic) ??
                throw new InvalidOperationException($"Handle Method not found for endpoint type");

        return CreateInvoker(baseEndpointType, handleMethod, requestType);
    }

    private static Func<object, object, Task> CreateInvoker(Type baseEndpointType, MethodInfo handleMethod, Type? requestType)
    {
        if (requestType is null)
            return CreateDelegate(baseEndpointType, typeof(EndpointContext), handleMethod);

        var handler = CreateDelegate(baseEndpointType, requestType, typeof(EndpointContext<>), handleMethod);
        return (endpointInst, context) =>
        {
            var endpointCtx = (context as IEndpointContext)!;
            var requestData = endpointCtx.Serializer.Deserialize(requestType, endpointCtx.Payload.Span) ??
                throw new NullReferenceException();

            return handler(endpointInst, requestData, context);
        };
    }

    private static Func<object, object, Task> CreateDelegate(
        Type baseEndpointType, Type contextType, MethodInfo handleMethod)
    {
        var instanceParam = Expression.Parameter(typeof(object));
        var contextParam = Expression.Parameter(typeof(object));

        var typedInstance = Expression.Convert(instanceParam, baseEndpointType);
        var typedContext = Expression.Convert(contextParam, contextType);

        var callExpression = Expression.Call(typedInstance, handleMethod, typedContext);

        var lambda = Expression.Lambda<Func<object, object, Task>>(
            callExpression, instanceParam, contextParam);

        return lambda.Compile();
    }

    private static Func<object, object, object, Task> CreateDelegate(
        Type baseEndpointType, Type requestType, Type contextType, MethodInfo handleMethod)
    {
        var instanceParam = Expression.Parameter(typeof(object));
        var messageParam = Expression.Parameter(typeof(object));
        var contextParam = Expression.Parameter(typeof(object));

        var typedInstance = Expression.Convert(instanceParam, baseEndpointType);
        var typedMessage = Expression.Convert(messageParam, requestType);
        var typedContext = Expression.Convert(contextParam, contextType);

        var callExpression = Expression.Call(typedInstance, handleMethod, typedMessage, typedContext);

        var lambda = Expression.Lambda<Func<object, object, object, Task>>(
            callExpression, instanceParam, messageParam, contextParam);

        return lambda.Compile();
    }
}
