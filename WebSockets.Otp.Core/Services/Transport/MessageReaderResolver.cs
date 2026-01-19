using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Services.Transport;

public sealed class MessageReaderResolver(IEnumerable<IMessageReader> messageReceivers) : IMessageReceiverResolver
{
    private readonly FrozenDictionary<string, IMessageReader> _store = messageReceivers
        .ToFrozenDictionary(c => c.ProtocolName);

    public bool TryResolve(ProcessProtocol protocol, [NotNullWhen(true)] out IMessageReader? messageReceiver) =>
        _store.TryGetValue(protocol, out messageReceiver);
}
