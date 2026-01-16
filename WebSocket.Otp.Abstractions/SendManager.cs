using System.Collections.Immutable;

namespace WebSockets.Otp.Abstractions;

public readonly struct SendManager<TResponse> 
    where TResponse : notnull
{
    private readonly ImmutableArray<string> _connectionIds;
    private readonly ImmutableArray<string> _groups;
    private readonly bool _targetAll;

    private SendManager(
       ImmutableArray<string> connectionIds,
       ImmutableArray<string> groups,
       bool targetAll)
    {
        _groups = groups;
        _targetAll = targetAll;
        _connectionIds = connectionIds;
    }

    public readonly SendManager<TResponse> Client(string connectionId)
    {
        if (_targetAll)
            return this;

        return new(
            _connectionIds.Add(connectionId),
            _groups,
            _targetAll);
    }

    public readonly SendManager<TResponse> Group(string groupName)
    {
        if (_targetAll)
            return this;

        return new(
            _connectionIds,
            _groups.Add(groupName),
            _targetAll);
    }

    public readonly SendManager<TResponse> All => new(_connectionIds, _groups, true);

    public readonly ValueTask SendAsync(TResponse data, CancellationToken token)
    {
        return ValueTask.CompletedTask;
    }
}
