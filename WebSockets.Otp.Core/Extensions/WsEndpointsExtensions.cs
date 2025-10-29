using System.Reflection;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Attributes;

namespace WebSockets.Otp.Core.Extensions;

public static class WsEndpointsExtensions
{
    public static bool IsWsEndpoint(this Type type) =>
        type is { IsAbstract: false } and { IsInterface: false } &&
        type.GetCustomAttribute<WsEndpointAttribute>() is not null &&
        type.GetBaseEndpointTypeSafe() is not null;

    public static bool AcceptsRequestMessages(this Type type) => GetRequestTypeSafe(type) is not null;

    public static Type GetRequestType(this Type type) =>
        GetRequestTypeSafe(type) ?? throw new ArgumentException($"Type '{nameof(type)}' not contains request type");

    public static Type GetBaseEndpointType(this Type type) =>
        GetBaseEndpointTypeSafe(type) ?? throw new ArgumentException($"Base type not found from '{nameof(type)}'");

    public static Type? GetRequestTypeSafe(this Type type)
    {
        Type? current = type;
        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(WsEndpoint<>))
                return current.GenericTypeArguments[0];

            current = current.BaseType;
        }
        return null;
    }

    private static Type? GetBaseEndpointTypeSafe(this Type type)
    {
        Type? current = type;
        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(WsEndpoint<>))
                return current;

            if (current == typeof(WsEndpoint))
                return current;

            current = current.BaseType;
        }

        return null;
    }
}
