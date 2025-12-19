using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Services.Serializers;

public sealed class DefaultSerializerResolver : ISerializerResolver
{
    private readonly FrozenDictionary<string, ISerializer> _store;

    public DefaultSerializerResolver(IEnumerable<ISerializer> serializers)
    {
        var store = new Dictionary<string, ISerializer>();

        foreach (var serializer in serializers)
            store[serializer.Format] = serializer;

        _store = store.ToFrozenDictionary();
    }

    public bool Registered(string format) => _store.ContainsKey(format);

    public bool TryResolve(string format, [NotNullWhen(true)] out ISerializer? serializer) =>
        _store.TryGetValue(format, out serializer);
}
