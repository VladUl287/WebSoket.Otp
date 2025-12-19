using System.Collections.Frozen;
using System.Reflection;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Extensions;

namespace WebSockets.Otp.Core.Services;

public sealed class WsEndpointRegistry : IWsEndpointRegistry
{
    private FrozenDictionary<string, Type> _map = new Dictionary<string, Type>().ToFrozenDictionary();

    public WsEndpointRegistry() { }
    public WsEndpointRegistry(IEnumerable<Type> types) => Register(types);

    public Type? Resolve(string path) => _map.TryGetValue(path, out var value) ? value : null;

    public IEnumerable<Type> Enumerate() => _map.Values.AsEnumerable();

    public void Register(IEnumerable<Type> types)
    {
        var mutated = _map.ToDictionary();
        foreach (var type in types)
        {
            var attr = ValidateWithException(type);
            mutated[attr.Key] = type;
        }
        _map = mutated.ToFrozenDictionary();
    }

    private static WsEndpointAttribute ValidateWithException(Type type)
    {
        var endpointAttr = type.GetCustomAttribute<WsEndpointAttribute>() ??
            throw new ArgumentException("Endpoint type must be annotated with [WsEndpoint(\"key\")]");

        if (!type.IsWsEndpoint())
            throw new ArgumentException("Type is not correct endpoint type");

        return endpointAttr;
    }
}
