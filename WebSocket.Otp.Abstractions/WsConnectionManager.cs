using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public readonly struct ConnectionManager(IEndpointExecutionContext context, IWsConnectionManager connectionManager)
{
    public readonly ValueTask SendAsync<TResponse>(string connectionId, TResponse data, CancellationToken token)
        where TResponse : notnull
    {
        var bytes = context.Serializer.Serialize(data);
        return connectionManager.SendAsync(connectionId, bytes, token);
    }

    public readonly ValueTask AddToGroupAsync(string groupName, string connectionId) =>
        connectionManager.AddToGroupAsync(connectionId, groupName);

    public readonly ValueTask RemoveFromGroupAsync(string connectionId, string groupName) =>
        connectionManager.RemoveFromGroupAsync(connectionId, groupName);
}
