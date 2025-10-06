namespace WebSockets.Otp.AspNet.Extensions;

public static class WsEndpointsExtensions
{
    public static bool IsWsEndpoint(this Type type)
    {
        return true;
    }    
    
    public static bool AcceptsRequestMessages(this Type type)
    {
        return true;
    }

    public static Type GetRequestType(this Type type)
    {
        return type;
    }
}
