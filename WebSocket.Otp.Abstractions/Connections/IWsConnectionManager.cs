namespace WebSockets.Otp.Abstractions.Connections;

public interface IWsConnectionManager
{
    bool TryAdd(IWsConnection connection);
    bool TryRemove(string connectionId);

    ValueTask<bool> AddToGroupAsync(string group, string connectionId);
    ValueTask<bool> RemoveFromGroupAsync(string group, string connectionId);

    ValueTask SendAsync<TData>(string connectionId, TData data, CancellationToken token)
        where TData : notnull;

    ValueTask SendAsync<TData>(IEnumerable<string> connections, TData data, CancellationToken token)
        where TData : notnull;

    ValueTask SendToGroupAsync<TData>(string group, TData data, CancellationToken token)
        where TData : notnull;

    ValueTask SendToGroupAsync<TData>(IEnumerable<string> groups, TData data, CancellationToken token)
        where TData : notnull;
}
