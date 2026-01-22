using WebSockets.Otp.Abstractions.Connections;

namespace WebSockets.Otp.Abstractions;

public readonly struct GroupManager(IWsConnectionManager manager)
{
    public readonly ValueTask<bool> AddAsync(string group, string connectionId, CancellationToken token = default) =>
        manager.AddToGroupAsync(group, connectionId, token);

    public readonly ValueTask<bool> RemoveAsync(string group, string connectionId, CancellationToken token = default) =>
        manager.RemoveFromGroupAsync(group, connectionId, token);
}
