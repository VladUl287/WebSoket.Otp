using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Results;

namespace WebSockets.Otp.AspNet.Authorization;

public sealed class AuthenticationValidator : IWsAuthorizationValidator
{
    public Task<WsAuthorizationResult> ValidateAsync(HttpContext context, WsAuthorizationOptions options)
    {
        if (context is { User.Identity.IsAuthenticated: true })
            return Task.FromResult(WsAuthorizationResult.Success());

        return Task.FromResult(WsAuthorizationResult.Failure("User not authenticated"));
    }
}
