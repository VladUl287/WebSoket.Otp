using System.Reflection;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Attributes;

namespace WebSockets.Otp.Core.Extensions;

public static class WsEndpointsExtensions
{
    public static bool IsWsEndpoint(this Type type) =>
        type is { IsAbstract: false } and { IsInterface: false } &&
        type.GetCustomAttribute<WsEndpointAttribute>() is not null &&
        type.GetBaseEndpointType() is not null;

    public static bool AcceptsRequestMessages(this Type type) => GetRequestType(type) is not null;

    public static Type? GetRequestType(this Type type)
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

    public static Type? GetBaseEndpointType(this Type type)
    {
        Type? current = type;
        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(WsEndpoint<,>))
                return current;

            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(WsEndpoint<>))
                return current;

            if (current == typeof(WsEndpoint))
                return current;

            current = current.BaseType;
        }

        return null;
    }
}
