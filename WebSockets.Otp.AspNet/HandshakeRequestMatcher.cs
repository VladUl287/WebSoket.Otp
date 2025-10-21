using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.AspNet;

public sealed class HandshakeRequestMatcher(string path) : IWsRequestMatcher
{
    public bool IsRequestMatch(HttpContext context) => context.Request.Path.Equals(path);
}
