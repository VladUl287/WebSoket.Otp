using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public sealed class SendManager(IWsConnectionManager manager)
{
    private readonly IWsConnectionManager _manager = manager;
    private readonly HashSet<string> _connectionIds = [];
    private readonly HashSet<string> _groups = [];
    private bool _targetAll = false;

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

    public SendManager All
    {
        get
        {
            _targetAll = true;
            return this;
        }
    }

    public ValueTask SendAsync<TResponse>(TResponse data, CancellationToken token)
    {
        return _manager.AddToGroupAsync(string.Empty, string.Empty);
    }
}

public sealed class SendManager<TResponse>(SendManager Manager)
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
