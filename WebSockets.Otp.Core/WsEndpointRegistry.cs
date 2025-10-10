using System.Collections.Frozen;
using System.Reflection;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Extensions;

namespace WebSockets.Otp.Core;

public sealed class WsEndpointRegistry : IWsEndpointRegistry
{
    private FrozenDictionary<string, Type> map = new Dictionary<string, Type>().ToFrozenDictionary();

    public Type? Resolve(string path) => map.TryGetValue(path, out var value) ? value : null;

    public IEnumerable<Type> Enumerate() => map.Values.AsEnumerable();

    public void Register(Type type)
    {
        var attr = ValidateWithException(type);
        var updated = map.ToDictionary();
        updated[attr.Key] = type;
        map = updated.ToFrozenDictionary();
    }

    public void Register(IEnumerable<Type> types)
    {
        var mutated = map.ToDictionary();
        foreach (var type in types)
        {
            var attr = ValidateWithException(type);
            mutated[attr.Key] = type;
        }
        map = mutated.ToFrozenDictionary();
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
