using Microsoft.AspNetCore.Http;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class PathSettings
{
    public PathString RequestPath { get; set; } = "/ws";
    public PathString HandshakePath { get; set; } = "/ws/_handshake";
}
