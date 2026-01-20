using System.Collections.Frozen;
using WebSockets.Otp.Abstractions.Enums;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services.Processors;

public sealed class MessageProcessorResolver(IEnumerable<IMessageProcessor> processors) : IMessageProcessorStore
{
    private readonly FrozenDictionary<ProcessingMode, IMessageProcessor> _store = processors.ToFrozenDictionary(c => c.Mode);

    public IMessageProcessor Get(ProcessingMode mode) => _store[mode];
}
