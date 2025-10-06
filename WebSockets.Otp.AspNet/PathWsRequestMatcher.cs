using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Helpers;

namespace WebSockets.Otp.AspNet;

public sealed class PathWsRequestMatcher(string path) : IWsRequestMatcher
{
    public bool IsWebSocketRequest(HttpContext context)
    {
        return context.WebSockets.IsWebSocketRequest && PathHelper.Normilize(context.Request.Path).Equals(path);
    }
}
