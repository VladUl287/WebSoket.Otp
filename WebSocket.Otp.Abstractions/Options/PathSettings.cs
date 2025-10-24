using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class PathSettings
{
    public PathString RequestPath { get; set; } = "/ws";
    public PathString HandshakePath { get; set; } = "/ws/_handshake";

    public IWsRequestMatcher RequestMatcher { get; set; } = default!;
}
