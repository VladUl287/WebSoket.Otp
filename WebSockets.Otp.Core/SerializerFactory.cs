using System.Collections.Frozen;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core;

public sealed class SerializerFactory : ISerializerFactory
{
    private readonly FrozenDictionary<string, ISerializer> _store;

    public SerializerFactory(IEnumerable<ISerializer> serializers)
    {
        var store = new Dictionary<string, ISerializer>();
        foreach (var serializer in serializers)
            store[serializer.Format] = serializer;
        _store = store.ToFrozenDictionary();
    }

    public ISerializer? TryResolve(string format)
    {
        if (_store.TryGetValue(format, out var serializer))
            return serializer;

        return null;
    }
}
