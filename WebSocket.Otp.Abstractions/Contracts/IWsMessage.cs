namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsMessage
{
    string Key { get; init; }
}

public abstract class WsMessage : IWsMessage
{
    private string _key = string.Empty;
    public string Key
    {
        get => _key;
        init => _key = string.Intern(value);
    }
}