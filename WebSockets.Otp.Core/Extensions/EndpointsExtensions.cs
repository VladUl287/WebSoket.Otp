using WebSockets.Otp.Abstractions;

namespace WebSockets.Otp.Core.Extensions;

public static class EndpointsExtensions
{
    public static bool IsWsEndpoint(this Type type) =>
        type is { IsAbstract: false, IsInterface: false, IsClass: true, IsPublic: true } &&
        typeof(IWsEndpoint).IsAssignableFrom(type);

    internal static Type? GetRequestType(this Type type)
    {
        Type? current = type;

        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(WsEndpoint<>))
                return current.GenericTypeArguments[0];

            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(WsEndpoint<,>))
                return current.GenericTypeArguments[0];

            current = current.BaseType;
        }
        return null;
    }

    internal static Type? GetBaseEndpointType(this Type type)
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
