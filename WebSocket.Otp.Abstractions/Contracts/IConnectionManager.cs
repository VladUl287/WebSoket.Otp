namespace WebSockets.Otp.Abstractions.Contracts;

public interface IConnectionManager
{
    bool TryAdd(IWsConnection connection);
    bool TryRemove(string connectionId);

    ValueTask AddToGroupAsync(string group, string connectionId);
    ValueTask RemoveFromGroupAsync(string group, string connectionId);

    ValueTask SendAsync<TData>(string connectionId, ReadOnlyMemory<byte> data, CancellationToken token) 
        where TData : notnull;

    ValueTask SendAsync<TData>(IEnumerable<string> connections, ReadOnlyMemory<byte> data, CancellationToken token) 
        where TData : notnull;

    ValueTask SendToGroupAsync<TData>(string group, ReadOnlyMemory<byte> data, CancellationToken token) 
        where TData : notnull;

    ValueTask SendToGroupAsync<TData>(IEnumerable<string> groups, ReadOnlyMemory<byte> data, CancellationToken token) 
        where TData : notnull;
}
