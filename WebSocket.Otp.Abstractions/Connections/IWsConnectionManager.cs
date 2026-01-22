namespace WebSockets.Otp.Abstractions.Connections;

public interface IWsConnectionManager
{
    ValueTask<bool> TryAdd(IWsConnection connection, CancellationToken token);
    ValueTask<bool> TryRemove(string connectionId, CancellationToken token);

    ValueTask<bool> AddToGroupAsync(string group, string connectionId, CancellationToken token);
    ValueTask<bool> RemoveFromGroupAsync(string group, string connectionId, CancellationToken token);

    ValueTask SendAsync<TData>(TData data, CancellationToken token)
        where TData : notnull;
    ValueTask SendAsync<TData>(string connectionId, TData data, CancellationToken token)
        where TData : notnull;
    ValueTask SendAsync<TData>(IEnumerable<string> connections, TData data, CancellationToken token)
        where TData : notnull;

    ValueTask SendToGroupAsync<TData>(string group, TData data, CancellationToken token)
        where TData : notnull;
    ValueTask SendToGroupAsync<TData>(IEnumerable<string> groups, TData data, CancellationToken token)
        where TData : notnull;
}
