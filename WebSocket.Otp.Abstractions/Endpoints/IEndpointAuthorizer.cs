using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Abstractions.Endpoints;

public interface IEndpointAuthorizer
{
    Task<Result<Success, string>> AuthorizeAsync(HttpContext context, WsEndpointInfo info);
}
