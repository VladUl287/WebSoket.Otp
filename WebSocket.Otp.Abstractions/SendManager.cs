using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Serializers;

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

public sealed class SendManager(
    EndpointHeaders headers, IMessageSerializer serializer, IWsConnectionManager manager) : SendManagerBase<SendManager>(manager)
{
    public async ValueTask SendAsync<TResponse>(TResponse data, CancellationToken token = default)
        where TResponse : notnull
    {
        if (_targetAll)
        {
            var bytes = serializer.SerializeToMessage(headers, data);
            await _manager.SendAsync(bytes, serializer.Type, token);
            return;
        }

        if (_connectionIds.Count > 0)
            await _manager.SendAsync(_connectionIds, data, token);

        if (_groups.Count > 0)
            await _manager.SendAsync(_groups, data, token);
    }
}

public sealed class SendManager<TResponse>(IWsConnectionManager manager) : SendManagerBase<SendManager<TResponse>>(manager)
    where TResponse : notnull
{
    public async ValueTask SendAsync(TResponse data, CancellationToken token = default)
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
