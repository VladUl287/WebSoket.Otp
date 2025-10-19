using System.Security.Claims;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions;

public interface IWsAuthorizationValidator
{
    public Task<WsAuthorizationResult> ValidateAsync(ClaimsPrincipal user, WsAuthorizationOptions options);
}