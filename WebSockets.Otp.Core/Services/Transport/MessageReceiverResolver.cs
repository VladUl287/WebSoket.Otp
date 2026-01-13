using System.Collections.Frozen;
using WebSockets.Otp.Abstractions.Contracts.Transport;

namespace WebSockets.Otp.Core.Services.Transport;

public sealed class MessageReceiverResolver(IEnumerable<IMessageReceiver> messageReceivers) : IMessageReceiverResolver
{
    private readonly FrozenDictionary<string, IMessageReceiver> _store = messageReceivers.ToFrozenDictionary(c => c.ProtocolName);

    public bool TryResolve(string format, out IMessageReceiver? messageReceiver) => _store.TryGetValue(format, out messageReceiver);
}
