using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Core.Services.Serializers;

public sealed class DefaultSerializerStore(IEnumerable<ISerializer> serializers) : ISerializerStore
{
    private readonly FrozenDictionary<string, ISerializer> _store = serializers.ToFrozenDictionary(c => c.ProtocolName);

    public bool TryGet(string format, [NotNullWhen(true)] out ISerializer? serializer) =>
        _store.TryGetValue(format, out serializer);
}
