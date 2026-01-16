using System.Collections.Immutable;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public readonly struct SendManager(
   IWsConnectionManager manager,
   ImmutableArray<string> connectionIds,
   ImmutableArray<string> groups,
   bool targetAll)
{
    private readonly IWsConnectionManager _manager = manager;
    private readonly ImmutableArray<string> _connectionIds = connectionIds;
    private readonly ImmutableArray<string> _groups = groups;
    private readonly bool _targetAll = targetAll;

    public readonly SendManager Client(string connectionId)
    {
        if (_targetAll) return this;

        return new(_manager, _connectionIds.Add(connectionId), _groups, _targetAll);
    }

    public readonly SendManager Group(string groupName)
    {
        if (_targetAll) return this;

        return new(_manager, _connectionIds, _groups.Add(groupName), _targetAll);
    }

    public readonly SendManager All => new(_manager, _connectionIds, _groups, true);

    public readonly ValueTask SendAsync<TResponse>(TResponse data, CancellationToken token)
    {
        return ValueTask.CompletedTask;
    }
}

public readonly struct SendManager<TResponse>(SendManager manager) where TResponse : notnull
{
    public readonly SendManager<TResponse> Client(string connectionId) =>
        new(manager.Client(connectionId));

    public readonly SendManager<TResponse> Group(string groupName) => 
        new(manager.Group(groupName));

    public readonly SendManager<TResponse> All => new(manager.All);

    public readonly ValueTask SendAsync(TResponse data, CancellationToken token) =>
        manager.SendAsync(data, token);
}
