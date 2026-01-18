using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Services.Endpoints;

public sealed class EndpointRegistry : IWsEndpointRegistry
{
    private FrozenDictionary<string, Type> _map = new Dictionary<string, Type>().ToFrozenDictionary();

    public EndpointRegistry() { }
    public EndpointRegistry(IEnumerable<Type> types) => Register(types);

    public bool TryResolve(string path, [NotNullWhen(true)] out Type? endpointType) => _map.TryGetValue(path, out endpointType);

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
