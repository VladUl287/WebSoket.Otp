using System.Reflection;
using System.Linq.Expressions;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Services.Endpoints;

public sealed class EndpointInvoker : IEndpointInvoker
{
    private readonly Func<object, object, Task> _handler;

    public EndpointInvoker(Type endpointType) => _handler = CreateHandleDelegate(endpointType);

    public Task Invoke(object endpoint, IEndpointContext context) =>
        _handler(endpoint, context);

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
