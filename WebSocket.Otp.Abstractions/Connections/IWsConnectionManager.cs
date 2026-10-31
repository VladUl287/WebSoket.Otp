using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Connections;

public interface IWsConnectionManager
{
    ValueTask<bool> TryAdd(IWsConnection connection, CancellationToken token);
    ValueTask<bool> TryRemove(string connectionId, CancellationToken token);

    ValueTask<bool> AddToGroup(string group, string connectionId, CancellationToken token);
    ValueTask<bool> RemoveFromGroup(string group, string connectionId, CancellationToken token);

    ValueTask Send<TData>(MessageEnvelope<TData> envelope, CancellationToken token) 
        where TData : notnull;
    ValueTask Send<TData>(string connectionId, MessageEnvelope<TData> envelope, CancellationToken token) 
        where TData : notnull;
    ValueTask Send<TData>(IEnumerable<string> connections, MessageEnvelope<TData> envelope, CancellationToken token) 
        where TData : notnull;

    ValueTask SendToGroup<TData>(string group, MessageEnvelope<TData> envelope, CancellationToken token) 
        where TData : notnull;
    ValueTask SendToGroup<TData>(IEnumerable<string> groups, MessageEnvelope<TData> envelope, CancellationToken token) 
        where TData : notnull;
}
