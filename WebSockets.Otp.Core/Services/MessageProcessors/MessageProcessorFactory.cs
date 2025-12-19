using System.Collections.Frozen;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Services.MessageProcessors;

public sealed class MessageProcessorFactory : IMessageProcessorFactory
{
    private readonly FrozenDictionary<string, IMessageProcessor> _store;

    public MessageProcessorFactory(IEnumerable<IMessageProcessor> processors)
    {
        var store = new Dictionary<string, IMessageProcessor>();
        foreach (var processor in processors)
            store[processor.Name] = processor;
        _store = store.ToFrozenDictionary();
    }

    public IMessageProcessor Create(string name) => _store[name];
}
