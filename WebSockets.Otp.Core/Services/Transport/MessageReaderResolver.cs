using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services.Transport;

public sealed class MessageReaderResolver(IEnumerable<IMessageReader> messageReceivers) : IMessageReaderStore
{
    private readonly FrozenDictionary<string, IMessageReader> _store = messageReceivers
        .ToFrozenDictionary(c => c.ProtocolName);

    public bool TryGet(string protocol, [NotNullWhen(true)] out IMessageReader? messageReceiver) =>
        _store.TryGetValue(protocol, out messageReceiver);
}
