using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public readonly struct GroupManager(IWsConnectionManager manager)
{
    public readonly ValueTask AddToGroupAsync<TGroup>(TGroup group, string connectionId)
    {
        return ValueTask.CompletedTask;
    }

    public readonly ValueTask RemoveFromGroupAsync<TGroup>(TGroup group, string connectionId)
    {
        return ValueTask.CompletedTask;
    }
}
