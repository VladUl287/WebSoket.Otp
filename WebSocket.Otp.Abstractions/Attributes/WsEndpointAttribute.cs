using System.Diagnostics;

namespace WebSockets.Otp.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class WsEndpointAttribute : Attribute
{
    public readonly string Key;

    public WsEndpointAttribute(string key)
    {
        Debug.WriteLine($"ctor::WsEndpointAttribute-{DateTime.Now}");
        Key = string.Intern(key);
    }
}
