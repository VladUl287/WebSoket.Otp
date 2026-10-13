using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions.Connections;

public interface IWsConnectionManager
{
    ValueTask<bool> TryAdd(IWsConnection connection, CancellationToken token);
    ValueTask<bool> TryRemove(string connectionId, CancellationToken token);

    ValueTask<bool> AddToGroupAsync(string group, string connectionId, CancellationToken token);
    ValueTask<bool> RemoveFromGroupAsync(string group, string connectionId, CancellationToken token);

    ValueTask SendAsync<TData>(EndpointHeaders headers, TData data, CancellationToken token);
    ValueTask SendAsync<TData>(string connectionId, EndpointHeaders headers, TData data, CancellationToken token);
    ValueTask SendAsync<TData>(IEnumerable<string> connections, EndpointHeaders headers, TData data, CancellationToken token);

    ValueTask SendToGroupAsync<TData>(string group, EndpointHeaders headers, TData data, CancellationToken token);
    ValueTask SendToGroupAsync<TData>(IEnumerable<string> groups, EndpointHeaders headers, TData data, CancellationToken token);
}
