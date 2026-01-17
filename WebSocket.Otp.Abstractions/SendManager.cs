using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public abstract class SendManagerBase(IWsConnectionManager manager)
{
    protected readonly IWsConnectionManager _manager = manager;
    protected readonly HashSet<string> _connectionIds = [];
    protected readonly HashSet<string> _groups = [];
    protected bool _targetAll = false;

    public SendManagerBase Client(string connectionId)
    {
        if (_targetAll) return this;
        _connectionIds.Add(connectionId);
        return this;
    }

    public SendManagerBase Group(string groupName)
    {
        if (_targetAll) return this;
        _groups.Add(groupName);
        return this;
    }

    public SendManagerBase All()
    {
        _targetAll = true;
        return this;
    }
}

public sealed class SendManager(IWsConnectionManager manager) : SendManagerBase(manager)
{
    public ValueTask SendAsync<TResponse>(TResponse data, CancellationToken token)
    {
        return _manager.AddToGroupAsync(string.Empty, string.Empty);
    }
}

public sealed class SendManager<TResponse>(IWsConnectionManager manager) : SendManagerBase(manager)
    where TResponse : notnull
{
    public ValueTask SendAsync(TResponse data, CancellationToken token)
    {
        return _manager.AddToGroupAsync(string.Empty, string.Empty);
    }
}
