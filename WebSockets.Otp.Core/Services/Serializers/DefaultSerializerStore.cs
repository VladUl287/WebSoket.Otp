using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Core.Services.Serializers;

public sealed class DefaultSerializerStore : ISerializerStore
{
    private readonly FrozenDictionary<string, IMessageSerializer> _store;

    public DefaultSerializerStore(IEnumerable<IMessageSerializer> serializers)
    {
        var last = new Dictionary<string, IMessageSerializer>(StringComparer.Ordinal);

        foreach (var s in serializers)
            last[s.Protocol] = s;

        _store = last.ToFrozenDictionary(StringComparer.Ordinal);
    }

    public bool TryGet(string format, [NotNullWhen(true)] out IMessageSerializer? serializer) =>
        _store.TryGetValue(format, out serializer);
}
