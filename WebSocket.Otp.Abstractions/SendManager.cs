using System.Collections.Immutable;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public readonly struct SendManager<TResponse>
    where TResponse : notnull
{
    private readonly IWsConnectionManager _manager;
    private readonly ImmutableArray<string> _connectionIds;
    private readonly ImmutableArray<string> _groups;
    private readonly bool _targetAll;

    private SendManager(
       IWsConnectionManager manager,
       ImmutableArray<string> connectionIds,
       ImmutableArray<string> groups,
       bool targetAll)
    {
        _manager = manager;
        _groups = groups;
        _targetAll = targetAll;
        _connectionIds = connectionIds;
    }

    public readonly SendManager<TResponse> Client(string connectionId)
    {
        if (_targetAll)
            return this;

        return new(
            _manager,
            _connectionIds.Add(connectionId),
            _groups,
            _targetAll);
    }

    public readonly SendManager<TResponse> Client(params IEnumerable<string> connections)
    {
        if (_targetAll)
            return this;

        return new(
            _manager,
            _connectionIds.AddRange(connections),
            _groups,
            _targetAll);
    }

    public readonly SendManager<TResponse> Group(string groupName)
    {
        if (_targetAll)
            return this;

        return new(
            _manager,
            _connectionIds,
            _groups.Add(groupName),
            _targetAll);
    }

    public readonly SendManager<TResponse> Group(params IEnumerable<string> groups)
    {
        if (_targetAll)
            return this;

        return new(
            _manager,
            _connectionIds,
            _groups.AddRange(groups),
            _targetAll);
    }

    public readonly SendManager<TResponse> All => new(_manager, _connectionIds, _groups, true);

    public readonly ValueTask SendAsync(TResponse data, CancellationToken token)
    {
        return ValueTask.CompletedTask;
    }
}
