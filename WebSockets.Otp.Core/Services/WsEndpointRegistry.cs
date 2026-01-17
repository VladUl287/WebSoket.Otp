using System.Collections.Frozen;
using System.Reflection;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Services;

public sealed class WsEndpointRegistry : IWsEndpointRegistry
{
    private FrozenDictionary<string, Type> _map = new Dictionary<string, Type>().ToFrozenDictionary();

    public WsEndpointRegistry() { }
    public WsEndpointRegistry(IEnumerable<Type> types) => Register(types);

    public Type? TryResolve(string path) => _map.TryGetValue(path, out var value) ? value : null;

    public IEnumerable<Type> Enumerate() => _map.Values.AsEnumerable();

    public void Register(IEnumerable<Type> types)
    {
        var mutated = _map.ToDictionary();
        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<WsEndpointAttribute>() ??
                throw new ArgumentException("Endpoint type must be annotated with [WsEndpoint(\"key\")]");
            mutated[attr.Key] = type;
        }
        _map = mutated.ToFrozenDictionary();
    }
}
