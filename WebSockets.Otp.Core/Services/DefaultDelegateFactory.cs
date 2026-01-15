using System.Linq.Expressions;
using System.Reflection;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Extensions;

namespace WebSockets.Otp.Core.Services;

public sealed class DefaultDelegateFactory : IHandleDelegateFactory
{
    private static readonly string MethodName = nameof(WsEndpoint.HandleAsync);
    private static readonly string MethodNotFoundMessage = $"Method '{nameof(WsEndpoint.HandleAsync)}' not found";
    private static readonly Type[] WithoutRequestTypes = [typeof(IWsExecutionContext), typeof(CancellationToken)];

    public Func<object, IWsExecutionContext, CancellationToken, Task> CreateHandleDelegate(Type endpointType)
    {
        var baseEndpType = endpointType.GetBaseEndpointType();
        var reqType = baseEndpType.GetRequestTypeSafe();

        var handleMethod = ResolveHandleMethod(baseEndpType, reqType);

        return CreateInvoker(baseEndpType, reqType, handleMethod);
    }

    public static MethodInfo ResolveHandleMethod(Type baseEndpType, Type? reqType)
    {
        var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

        if (reqType is not null)
        {
            Type[] types = [reqType, typeof(IWsExecutionContext), typeof(CancellationToken)];
            return baseEndpType.GetMethod(MethodName, flags, types: types) ??
                throw new InvalidOperationException(MethodNotFoundMessage);
        }

        var handleMethod = baseEndpType.GetMethod(MethodName, flags, types: WithoutRequestTypes);
        return handleMethod ??
            throw new InvalidOperationException(MethodNotFoundMessage);
    }

    private static Func<object, IWsExecutionContext, CancellationToken, Task> CreateInvoker(Type baseEndpType, Type? reqType, MethodInfo handleMethod)
    {
        if (reqType is null)
            return CreateHandlerDelegate(baseEndpType, handleMethod);

        return CreateRequestHandlingInvoker(baseEndpType, reqType, handleMethod);
    }

    private static Func<object, IWsExecutionContext, CancellationToken, Task> CreateRequestHandlingInvoker(Type baseEndpType, Type reqType, MethodInfo handleMethod)
    {
        var handler = CreateHandlerDelegate(baseEndpType, reqType, handleMethod);
        return (endpointInst, execCtx, token) =>
        {
            var requestData = execCtx.Serializer.Deserialize(reqType, execCtx.Payload.Span) ??
                throw new NullReferenceException();

            return handler(endpointInst, requestData, execCtx, token);
        };
    }

    private static Func<object, IWsExecutionContext, CancellationToken, Task> CreateHandlerDelegate(Type baseType, MethodInfo handleMethod)
    {
        var instanceParam = Expression.Parameter(typeof(object));
        var contextParam = Expression.Parameter(typeof(IWsExecutionContext));
        var cancellationParam = Expression.Parameter(typeof(CancellationToken));

        var typedInstance = Expression.Convert(instanceParam, baseType);

        var callExpression = Expression.Call(typedInstance, handleMethod, contextParam, cancellationParam);

        var lambda = Expression.Lambda<Func<object, IWsExecutionContext, CancellationToken, Task>>(
            callExpression, instanceParam, contextParam, cancellationParam);

        return lambda.Compile();
    }

    private static Func<object, object, IWsExecutionContext, CancellationToken, Task> CreateHandlerDelegate(
        Type baseType, Type requestType, MethodInfo handleMethod)
    {
        var instanceParam = Expression.Parameter(typeof(object));
        var messageParam = Expression.Parameter(typeof(object));
        var contextParam = Expression.Parameter(typeof(IWsExecutionContext));
        var cancellationParam = Expression.Parameter(typeof(CancellationToken));

        var typedInstance = Expression.Convert(instanceParam, baseType);
        var typedMessage = Expression.Convert(messageParam, requestType);

        var callExpression = Expression.Call(typedInstance, handleMethod, typedMessage, contextParam, cancellationParam);

        var lambda = Expression.Lambda<Func<object, object, IWsExecutionContext, CancellationToken, Task>>(
            callExpression, instanceParam, messageParam, contextParam, cancellationParam);

        return lambda.Compile();
    }
}
