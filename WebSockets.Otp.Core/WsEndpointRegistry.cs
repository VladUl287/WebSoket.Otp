using System.Collections.Concurrent;
using System.Reflection;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core;

public sealed class WsEndpointRegistry : IWsEndpointRegistry
{
    private readonly ConcurrentDictionary<string, Type> map = new();

    public Type? Resolve(string path) => map.TryGetValue(path, out var value) ? value : null;

    public IEnumerable<Type> Enumerate() => map.Values.AsEnumerable();

    public void Register(Type type)
    {
        var endpointAttr = type.GetCustomAttribute<WsEndpointAttribute>();
        if (endpointAttr is not { } attr)
            throw new ArgumentException("Endpoint type must be annotated with [WsEndpoint(\"route\")]");

        //TODO: refactor concrete endpoint types from method
        var baseType = type.BaseType;
        if (baseType is null or { IsAbstract: false })
            throw new ArgumentException("Endpoint type must be annotated with [WsEndpoint(\"route\")]");

        if (!baseType.IsGenericType && baseType != typeof(WsEndpoint))
            throw new ArgumentException("Endpoint type must be annotated with [WsEndpoint(\"route\")]");

        if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() != typeof(WsEndpoint<>))
            throw new ArgumentException("Endpoint type must be annotated with [WsEndpoint(\"route\")]");

        map[attr.Route] = type;
    }
}
