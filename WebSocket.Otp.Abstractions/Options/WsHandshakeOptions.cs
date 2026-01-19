namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsHandshakeOptions
{
    public string Protocol { get; set; } = "json";

    //public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromMinutes(2);
    //public int MaxConnections { get; set; } = 1000;
    //public int MaxConnectionsPerUser { get; set; } = 10;
}
