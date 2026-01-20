using WebSockets.Otp.Abstractions.Connections;

namespace WebSockets.Otp.Abstractions;

public readonly struct GroupManager(IWsConnectionManager manager)
{
    public readonly ValueTask<bool> AddAsync(string group, string connectionId) =>
        manager.AddToGroupAsync(group, connectionId);

    public readonly ValueTask<bool> RemoveAsync(string group, string connectionId) =>
        manager.RemoveFromGroupAsync(group, connectionId);
}
