using WebSockets.Otp.Abstractions.Connections;

namespace WebSockets.Otp.Abstractions;

public readonly struct GroupManager(IWsConnectionManager manager)
{
    public readonly ValueTask AddAsync(string group, string connectionId) =>
        manager.AddToGroupAsync(group, connectionId);

    public readonly ValueTask RemoveAsync(string group, string connectionId) =>
        manager.RemoveFromGroupAsync(group, connectionId);
}
