using System.Reflection;
using System.Linq.Expressions;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Abstractions.Pipeline;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Pipeline.Steps;

public sealed class EndpointStep(Type endpointType) : IPipelineStep
{
    public Task ProcessAsync(object endpoint, IEndpointContext context)
    {
        if (endpointType == typeof(WsEndpoint))
            return (endpoint as WsEndpoint)!.HandleAsync((context as EndpointContext)!);

        var baseEndpointType = endpointType.GetBaseEndpointType();
        if (baseEndpointType.GetGenericTypeDefinition() == typeof(WsEndpoint<>))
        {
            var requestType = endpointType.GetRequestType();
            Type[] types = [requestType, typeof(EndpointContext)];

            var handleMethod = baseEndpointType.GetMethod(
                nameof(WsEndpoint.HandleAsync), BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic, types) ??
                throw new InvalidOperationException("Method not found");

            var invoker = CreateDelegate(baseEndpointType, requestType, handleMethod);

            var requestData = context.Serializer.Deserialize(requestType, context.Payload.Span);

            return invoker(endpoint, requestData, (context as EndpointContext)!);
        }

        if (baseEndpointType.GetGenericTypeDefinition() == typeof(WsEndpoint<,>))
        {
            var requestType = endpointType.GetRequestType();
            Type[] types = [requestType, typeof(EndpointContext)];

            var handleMethod = baseEndpointType.GetMethod(
                nameof(WsEndpoint.HandleAsync), BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic, types) ??
                throw new InvalidOperationException("Method not found");

            var invoker = CreateDelegate(baseEndpointType, requestType, handleMethod);

            var requestData = context.Serializer.Deserialize(requestType, context.Payload.Span);

            return invoker(endpoint, requestData, (context as EndpointContext)!);
        }

        return Task.CompletedTask;
    }

    private static Func<object, object, EndpointContext, Task> CreateDelegate(
        Type endpointType, Type requestType, MethodInfo handleMethod)
    {
        var instanceParam = Expression.Parameter(typeof(object));
        var messageParam = Expression.Parameter(typeof(object));
        var contextParam = Expression.Parameter(typeof(EndpointContext));

        var typedInstance = Expression.Convert(instanceParam, endpointType);
        var typedMessage = Expression.Convert(messageParam, requestType);

        var callExpression = Expression.Call(typedInstance, handleMethod, typedMessage, contextParam);

        var lambda = Expression.Lambda<Func<object, object, EndpointContext, Task>>(
            callExpression, instanceParam, messageParam, contextParam);

        return lambda.Compile();
    }
}
