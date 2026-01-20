using WebSockets.Otp.Abstractions.Connections;

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
    public async ValueTask SendAsync<TResponse>(TResponse data, CancellationToken token)
        where TResponse : notnull
    {
        if (_targetAll)
        {
            await _manager.SendAsync(data, token);
            return;
        }

        if (_connectionIds.Count > 0)
            await _manager.SendAsync(_connectionIds, data, token);

        if (_groups.Count > 0)
            await _manager.SendAsync(_groups, data, token);
    }
}

public sealed class SendManager<TResponse>(IWsConnectionManager manager) : SendManagerBase<SendManager>(manager)
    where TResponse : notnull
{
    public async ValueTask SendAsync(TResponse data, CancellationToken token)
    {
        if (_targetAll)
        {
            await _manager.SendAsync(data, token);
            return;
        }

        if (_connectionIds.Count > 0)
            await _manager.SendAsync(_connectionIds, data, token);

        if (_groups.Count > 0)
            await _manager.SendAsync(_groups, data, token);
    }
}

//public abstract class SendManagerBase<TDerived>(IWsConnectionManager manager)
//{
//    protected readonly IWsConnectionManager _manager = manager;
//    protected readonly HashSet<string> _connectionIds = [];
//    protected readonly HashSet<string> _groups = [];
//    protected bool _targetAll = false;

//    protected internal SendManagerBase<TDerived> AddClient(string connectionId)
//    {
//        if (!_targetAll)
//            _connectionIds.Add(connectionId);
//        return this;
//    }

//    protected internal SendManagerBase<TDerived> AddGroup(string groupName)
//    {
//        if (!_targetAll)
//            _groups.Add(groupName);
//        return this;
//    }

//    protected internal SendManagerBase<TDerived> SetAll()
//    {
//        _targetAll = true;
//        return this;
//    }

//    public abstract TDerived Client(string connectionId);
//    public abstract TDerived Group(string groupName);
//    public abstract TDerived All();
//}