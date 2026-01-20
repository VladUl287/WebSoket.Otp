using WebSockets.Otp.Abstractions.Connections;

namespace WebSockets.Otp.Abstractions;

public sealed class GroupManager(IWsConnectionManager manager)
{
    public IWsConnectionManager Manager { get; init; } = manager;

    public ValueTask AddToGroupAsync(string group, string connectionId)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveFromGroupAsync(string group, string connectionId)
    {
        return ValueTask.CompletedTask;
    }
}
