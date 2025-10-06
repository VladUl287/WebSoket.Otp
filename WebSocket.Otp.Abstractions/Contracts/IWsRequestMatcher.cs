using Microsoft.AspNetCore.Http;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsRequestMatcher
{
    bool IsWebSocketRequest(HttpContext context);
}
