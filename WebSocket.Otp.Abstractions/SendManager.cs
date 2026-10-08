using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Abstractions;

public sealed class SendManager(IMessageSerializer serializer, IWsConnectionManager manager)
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
        var messageBytes = serializer.SerializeToMessage(new EndpointHeaders { Key = key }, data);

        if (_targetAll)
        {
            await _manager.SendAsync(messageBytes, serializer.Type, token);
            return;
        }

        if (_connectionIds.Count > 0)
            await _manager.SendAsync(_connectionIds, messageBytes, serializer.Type, token);

        if (_groups.Count > 0)
            await _manager.SendAsync(_groups, messageBytes, serializer.Type, token);
    }
}
