using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Core.Services.Serializers;

public sealed class DefaultSerializerStore(IEnumerable<IMessageSerializer> serializers) : ISerializerStore
{
    private readonly FrozenDictionary<string, IMessageSerializer> _store = serializers.ToFrozenDictionary(c => c.Protocol);

    public bool TryGet(string format, [NotNullWhen(true)] out IMessageSerializer? serializer) =>
        _store.TryGetValue(format, out serializer);
}
