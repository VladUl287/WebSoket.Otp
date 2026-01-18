using System.Collections.Frozen;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services.MessageProcessors;

public sealed class MessageProcessorResolver(IEnumerable<IMessageProcessor> processors) : IMessageProcessorResolver
{
    private readonly FrozenDictionary<string, IMessageProcessor> _store = processors.ToFrozenDictionary(c => c.Mode);

    public bool CanResolve(string mode) => _store.ContainsKey(mode);

    public IMessageProcessor Resolve(string mode) => _store[mode];
}
