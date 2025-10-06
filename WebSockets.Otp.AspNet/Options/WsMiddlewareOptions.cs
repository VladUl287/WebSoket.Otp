using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.AspNet.Options;

public sealed class WsMiddlewareOptions
{
    public string Path { get; set; } = string.Empty;

    public long MaxReceiveMessageSize { get; set; } = 64 * 1024; //64kb

    public IWsRequestMatcher RequestMatcher { get; set; } = default!;
}
