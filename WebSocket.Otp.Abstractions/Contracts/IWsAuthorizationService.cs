using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Results;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsAuthorizationService
{
    Task<WsAuthorizationResult> AuhtorizeAsync(HttpContext context, AuthorizationSettings options);
}
