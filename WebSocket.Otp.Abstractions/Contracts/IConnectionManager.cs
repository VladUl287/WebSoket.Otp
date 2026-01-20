namespace WebSockets.Otp.Abstractions.Contracts;

public interface IConnectionManager
{
    bool TryAdd(IWsConnection connection);
    bool TryRemove(string connectionId);

    ValueTask AddToGroupAsync(string group, string connectionId);
    ValueTask RemoveFromGroupAsync(string group, string connectionId);

    ValueTask SendAsync<TData>(string connectionId, TData data, CancellationToken token)
        where TData : notnull;

    ValueTask SendAsync<TData>(IEnumerable<string> connections, TData data, CancellationToken token)
        where TData : notnull;

    ValueTask SendToGroupAsync<TData>(string group, TData data, CancellationToken token)
        where TData : notnull;

    ValueTask SendToGroupAsync<TData>(IEnumerable<string> groups, TData data, CancellationToken token)
        where TData : notnull;
}
