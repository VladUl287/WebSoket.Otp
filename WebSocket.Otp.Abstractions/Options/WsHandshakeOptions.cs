namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsHandshakeOptions
{
    public string Protocol { get; init; } = "json";
}
