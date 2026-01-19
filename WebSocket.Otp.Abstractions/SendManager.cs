using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public abstract class SendManagerBase<TDerived>(IWsConnectionManager manager)
    where TDerived : SendManagerBase<TDerived>
{
    protected readonly IWsConnectionManager _manager = manager;
    protected readonly HashSet<string> _connectionIds = [];
    protected readonly HashSet<string> _groups = [];
    protected bool _targetAll = false;

    public TDerived Client(string connectionId)
    {
        if (_targetAll) return (TDerived)this;
        _connectionIds.Add(connectionId);
        return (TDerived)this;
    }

    public TDerived Group(string groupName)
    {
        if (_targetAll) return (TDerived)this;
        _groups.Add(groupName);
        return (TDerived)this;
    }

    public TDerived All()
    {
        _targetAll = true;
        return (TDerived)this;
    }
}

public sealed class SendManager(IWsConnectionManager manager) : SendManagerBase<SendManager>(manager)
{
    public ValueTask SendAsync<TResponse>(TResponse data, CancellationToken token)
    {
        return _manager.AddToGroupAsync(string.Empty, string.Empty);
    }
}

public sealed class SendManager<TResponse>(IWsConnectionManager manager) : SendManagerBase<SendManager>(manager)
    where TResponse : notnull
{
    public ValueTask SendAsync(TResponse data, CancellationToken token)
    {
        return _manager.AddToGroupAsync(string.Empty, string.Empty);
    }
}
