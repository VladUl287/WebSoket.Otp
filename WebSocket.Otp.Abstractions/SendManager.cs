using System.Collections.Immutable;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public sealed record SendManager(
   IWsConnectionManager Manager,
   ImmutableArray<string> ConnectionIds,
   ImmutableArray<string> Groups,
   bool TargetAll)
{
    private readonly IWsConnectionManager _manager = Manager;
    private readonly ImmutableArray<string> _connectionIds = ConnectionIds;
    private readonly ImmutableArray<string> _groups = Groups;
    private readonly bool _targetAll = TargetAll;

    public SendManager Client(string connectionId)
    {
        if (_targetAll) return this;

        return new(_manager, _connectionIds.Add(connectionId), _groups, _targetAll);
    }

    public SendManager Group(string groupName)
    {
        if (_targetAll) return this;

        return new(_manager, _connectionIds, _groups.Add(groupName), _targetAll);
    }

    public SendManager All => new(_manager, _connectionIds, _groups, true);

    public ValueTask SendAsync<TResponse>(TResponse data, CancellationToken token)
    {
        return ValueTask.CompletedTask;
    }
}

public sealed record SendManager<TResponse>(SendManager Manager) 
    where TResponse : notnull
{
    public SendManager<TResponse> Client(string connectionId) =>
        new(Manager.Client(connectionId));

    public SendManager<TResponse> Group(string groupName) => 
        new(Manager.Group(groupName));

    public SendManager<TResponse> All => new(Manager.All);

    public ValueTask SendAsync(TResponse data, CancellationToken token) =>
        Manager.SendAsync(data, token);
}
