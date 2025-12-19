using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsAuthorizationService
{
    ValueTask<bool> TryAuhtorize(HttpContext context, WsAuthorizationOptions options);
}
