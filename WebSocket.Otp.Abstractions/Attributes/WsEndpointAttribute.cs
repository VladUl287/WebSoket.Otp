namespace WebSockets.Otp.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class WsEndpointAttribute(string route) : Attribute
{
    public string Route => route;
}
