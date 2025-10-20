namespace WebSockets.Otp.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class WsEndpointAttribute(string key) : Attribute
{
    public string Key => key;
}
