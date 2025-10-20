using Microsoft.AspNetCore.Http;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsRequestMatcher
{
    bool IsRequestMatch(HttpContext context);
}
