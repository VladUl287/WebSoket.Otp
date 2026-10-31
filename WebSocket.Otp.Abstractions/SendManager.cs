using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions;

public sealed class SendManager(IWsConnectionManager manager)
{
    public readonly IWsConnectionManager _manager = manager;
    public readonly HashSet<string> _connectionIds = [];
    public readonly HashSet<string> _groups = [];
    public bool _targetAll = false;

    public SendManager Client(string connectionId)
    {
        if (_targetAll) return this;
        _connectionIds.Add(connectionId);
        return this;
    }

    public SendManager Group(string groupName)
    {
        if (_targetAll) return this;
        _groups.Add(groupName);
        return this;
    }

    public SendManager All()
    {
        _targetAll = true;
        return this;
    }

    public async ValueTask SendAsync<TResponse>(string key, TResponse data, CancellationToken token = default)
        where TResponse : notnull
    {
        var envelope = new MessageEnvelope<TResponse>(key, data);

        try
        {
            if (_targetAll)
            {
                await _manager.Send(envelope, token);
                return;
            }

            if (_connectionIds.Count > 0)
                await _manager.Send(_connectionIds, envelope, token);

            if (_groups.Count > 0)
                await _manager.SendToGroup(_groups, envelope, token);
        }
        finally
        {
            _connectionIds.Clear();
            _groups.Clear();
            _targetAll = false;
        }
    }
}
