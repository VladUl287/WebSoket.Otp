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
        var baseEndpointType = endpointType.GetBaseEndpointType();

        if (baseEndpointType == typeof(WsEndpoint))
            return (endpoint as WsEndpoint)!.HandleAsync((context as EndpointContext)!);

        var requestType = endpointType.GetRequestType();

        var handleMethod = baseEndpointType.GetMethod(
            nameof(WsEndpoint.HandleAsync), BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic) ??
                throw new InvalidOperationException($"Handle Method not found for endpoint type '{endpointType}'");

        var requestData = context.Serializer.Deserialize(requestType, context.Payload.Span);

        var baseDefinition = baseEndpointType.GetGenericTypeDefinition();

        if (baseDefinition == typeof(WsEndpoint<>))
        {
            var invoker = CreateDelegate(baseEndpointType, requestType, typeof(EndpointContext), handleMethod);
            return invoker(endpoint, requestData, context);
        }

        if (baseDefinition == typeof(WsEndpoint<,>))
        {
            var invoker = CreateDelegate(baseEndpointType, requestType, typeof(EndpointContext<>), handleMethod);
            return invoker(endpoint, requestData, context);
        }

        throw new InvalidOperationException();
    }

    private static Func<object, object, object, Task> CreateDelegate(
        Type endpointType, Type requestType, Type contextType, MethodInfo handleMethod)
    {
        var instanceParam = Expression.Parameter(typeof(object));
        var messageParam = Expression.Parameter(typeof(object));
        var contextParam = Expression.Parameter(typeof(object));

        var typedInstance = Expression.Convert(instanceParam, endpointType);
        var typedMessage = Expression.Convert(messageParam, requestType);
        var typedContext = Expression.Convert(contextParam, contextType);

        var callExpression = Expression.Call(typedInstance, handleMethod, typedMessage, typedContext);

        var lambda = Expression.Lambda<Func<object, object, object, Task>>(
            callExpression, instanceParam, messageParam, contextParam);

        return lambda.Compile();
    }
}
