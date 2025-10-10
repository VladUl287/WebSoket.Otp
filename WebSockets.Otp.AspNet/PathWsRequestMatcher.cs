using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.AspNet;

public sealed class PathWsRequestMatcher(string path) : IWsRequestMatcher
{
    public bool IsWebSocketRequest(HttpContext context) =>
        context.WebSockets.IsWebSocketRequest && context.Request.Path.Equals(path);
}
