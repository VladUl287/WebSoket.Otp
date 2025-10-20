using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Results;

namespace WebSockets.Otp.AspNet.Authorization;

public sealed class AuthenticationValidator : IWsAuthorizationValidator
{
    public Task<AuthValidationResult> ValidateAsync(HttpContext context, WsAuthorizationOptions options)
    {
        if (context is { User.Identity.IsAuthenticated: true })
            return Task.FromResult(AuthValidationResult.Success());

        return Task.FromResult(AuthValidationResult.Failure(StatusCodes.Status401Unauthorized, "User not authenticated"));
    }
}
