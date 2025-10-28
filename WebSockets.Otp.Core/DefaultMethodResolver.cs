using System.Reflection;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Extensions;

namespace WebSockets.Otp.Core;

public sealed class DefaultMethodResolver : IMethodResolver
{
    public MethodInfo ResolveHandleMethod(Type endpointType)
    {
        if (endpointType.AcceptsRequestMessages())
            return ResolveRequestHandleMethod(endpointType);

        return ResolveConnectionHandleMethod(endpointType);
    }

    public MethodInfo ResolveHandleMethodFromBase(Type endpointType)
    {
        var handleMethod = endpointType.GetBaseEndpointType().GetMethod(
            nameof(WsEndpoint.HandleAsync),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [typeof(IWsExecutionContext), typeof(CancellationToken)],
            null);

        return handleMethod ??
            throw new InvalidOperationException("Method 'HandleAsync(IWsConnection, CancellationToken)' not found");
    }

    private static MethodInfo ResolveRequestHandleMethod(Type endpointType)
    {
        var baseType = endpointType.GetBaseEndpointType();
        var requestType = baseType.GenericTypeArguments[0];

        var handleMethod = endpointType.GetMethod(
            nameof(WsEndpoint.HandleAsync),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [requestType, typeof(IWsExecutionContext), typeof(CancellationToken)],
            null);

        return handleMethod ??
            throw new InvalidOperationException($"Method 'HandleAsync({requestType.Name}, IWsExecutionContext, CancellationToken)' not found");
    }

    private static MethodInfo ResolveConnectionHandleMethod(Type endpointType)
    {
        var handleMethod = endpointType.GetMethod(
            nameof(WsEndpoint.HandleAsync),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [typeof(IWsExecutionContext), typeof(CancellationToken)],
            null);

        return handleMethod ??
            throw new InvalidOperationException("Method 'HandleAsync(IWsConnection, CancellationToken)' not found");
    }
}
