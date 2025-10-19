using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions;

public interface IWsAuthorizationService
{
    Task<bool> AuhtorizeAsync(HttpContext context, WsAuthorizationOptions options);
}
