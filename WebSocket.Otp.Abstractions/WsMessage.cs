namespace WebSockets.Otp.Abstractions;

public abstract class WsMessage
{
    public string Key { get; init; } = string.Empty;
}
