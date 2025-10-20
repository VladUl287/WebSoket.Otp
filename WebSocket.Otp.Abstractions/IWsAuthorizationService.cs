using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Results;

namespace WebSockets.Otp.Abstractions;

public interface IWsAuthorizationService
{
    Task<WsAuthorizationResult> AuhtorizeAsync(HttpContext context, WsAuthorizationOptions options);
}
