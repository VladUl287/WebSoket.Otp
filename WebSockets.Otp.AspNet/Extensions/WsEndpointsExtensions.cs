using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.AspNet.Extensions;

public static class WsEndpointsExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWsEndpoint(this Type type) => type.IsAssignableTo(typeof(IWsEndpoint));

    public static bool AcceptsRequestMessages(this Type type)
    {
        if (!type.IsWsEndpoint())
            return false;

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
        return type;
    }
}
