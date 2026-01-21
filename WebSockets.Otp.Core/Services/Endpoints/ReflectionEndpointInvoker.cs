using System.Linq.Expressions;
using System.Reflection;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Core.Extensions;

namespace WebSockets.Otp.Core.Services.Endpoints;

public sealed class ReflectionEndpointInvoker(Type endpointType) : IEndpointInvoker
{
    private readonly Func<object, object, Task> _handler = CreateHandleDelegate(endpointType);

    public Task Invoke(object endpoint, IEndpointContext context) => _handler(endpoint, context);

    public static Func<object, object, Task> CreateHandleDelegate(Type endpointType)
    {
        var baseEndpointType = endpointType.GetBaseEndpointType()!;

        var handleMethod = baseEndpointType.GetMethod(
            nameof(WsEndpoint.HandleAsync), BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic) ??
                throw new InvalidOperationException($"Handle Method not found for endpoint type");

        return CreateInvoker(baseEndpointType, handleMethod);
    }

    private static Func<object, object, Task> CreateInvoker(Type baseEndpointType, MethodInfo handleMethod)
    {
        if (baseEndpointType == typeof(WsEndpoint))
            return CreateDelegate(baseEndpointType, typeof(EndpointContext), handleMethod);

        var contextType = baseEndpointType.GetGenericTypeDefinition() == typeof(WsEndpoint<>) ?
            typeof(EndpointContext) :
            typeof(EndpointContext<>);

        var requestType = baseEndpointType.GetRequestType()!;
        var handler = CreateDelegate(baseEndpointType, requestType, contextType, handleMethod);

        return (endpointInst, context) =>
        {
            var endpointContext = (BaseEndpointContext)context;
            var requestData = endpointContext.Serializer.Deserialize(requestType, endpointContext.Payload.Span) ??
                throw new NullReferenceException($"Fail to deserialize message for endpoint '{endpointInst.GetType()}'");
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
