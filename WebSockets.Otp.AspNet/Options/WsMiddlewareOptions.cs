using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.AspNet.Options;

public sealed class WsMiddlewareOptions
{
    public string RequestPath { get; set; } = string.Empty;

    public long MaxMessageSize { get; set; } = 64 * 1024; //64kb

    public int InitialBufferSize { get; set; } = 8 * 1024; // 8KB

    public bool ReclaimBufferAfterEachMessage { get; set; } = true;

    public IWsRequestMatcher RequestMatcher { get; set; } = default!;
}
