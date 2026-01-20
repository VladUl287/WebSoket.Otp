using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public sealed class GroupManager(IConnectionManager manager)
{
    public IConnectionManager Manager { get; init; } = manager;

    public ValueTask AddToGroupAsync(string group, string connectionId)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveFromGroupAsync(string group, string connectionId)
    {
        return ValueTask.CompletedTask;
    }
}
