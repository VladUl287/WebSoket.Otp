using System.Security.Claims;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class ConnectionSettings
{
    public ClaimsPrincipal? User { get; set; }
    public WsProtocol Protocol { get; set; } = WsProtocol.Json;
    //public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromMinutes(2);
    //public int MaxConnections { get; set; } = 1000;
    //public int MaxConnectionsPerUser { get; set; } = 10;
}

public sealed class WsProtocol : IEquatable<WsProtocol>
{
    public string Value { get; }
    private WsProtocol(string value) => Value = value;

    public static readonly WsProtocol Json = new("json");
    //public static readonly WsProtocol Protobuf = new("proto");

    public static WsProtocol New(string value) => new(value);

    public override string ToString() => Value;
    public bool Equals(WsProtocol? other) => other?.Value == Value;
    public override bool Equals(object? obj) => obj is WsProtocol other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(WsProtocol? left, WsProtocol? right) =>
        Equals(left, right);
    public static bool operator !=(WsProtocol? left, WsProtocol? right) =>
        !Equals(left, right);

    public static implicit operator string(WsProtocol mode) => mode.Value;
}