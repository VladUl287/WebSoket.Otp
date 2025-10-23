using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.AspNet;

public sealed class DefaltWsRequestMatcher(string path) : IWsRequestMatcher
{
    public bool IsRequestMatch(HttpContext context) =>
        context.WebSockets.IsWebSocketRequest && context.Request.Path.Equals(path);
}
