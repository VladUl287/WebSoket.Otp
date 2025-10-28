using System.Reflection;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Extensions;

public static class WsEndpointsExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWsEndpoint(this Type type) =>
        type is { IsAbstract: false } && 
        type.IsAssignableTo(typeof(IWsEndpoint)) && 
        type.GetCustomAttribute<WsEndpointAttribute>() is not null;

    public static bool AcceptsRequestMessages(this Type type)
    {
        while (type is not null)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(WsEndpoint<>))
                return true;

            type = type.BaseType;
        }

        return false;
    }

    public static Type GetRequestType(this Type type)
    {
        while (type is not null)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(WsEndpoint<>))
                return type.GenericTypeArguments[0];

            type = type.BaseType;
        }

        throw new ArgumentException($"Type '{nameof(type)}' not contains request type");
    }

    public static Type GetBaseEndpointType(this Type type)
    {
        while (type is not null)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(WsEndpoint<>))
                return type;

            if (type == typeof(WsEndpoint))
                return type;

            type = type.BaseType;
        }

        throw new ArgumentException($"Base type not found from '{nameof(type)}'");
    }
}
