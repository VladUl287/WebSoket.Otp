using Microsoft.Extensions.DependencyInjection;

namespace WebSockets.Otp.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class WsEndpointAttribute(string key, ServiceLifetime scope = ServiceLifetime.Scoped) : Attribute
{
    public string Key => key;
    public ServiceLifetime Scope => scope;
}
