using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Results;

namespace WebSockets.Otp.AspNet.Authorization;

public sealed class SchemeValidator : IWsAuthorizationValidator
{
    public Task<AuthValidationResult> ValidateAsync(HttpContext context, WsAuthorizationOptions options)
    {
        var authScheme = context.User.Identity?.AuthenticationType;
        if (options is { Schemes.Length: 0 } || options.Schemes.Contains(authScheme))
            return Task.FromResult(AuthValidationResult.Success());

        return Task.FromResult(AuthValidationResult.Failure(StatusCodes.Status403Forbidden, ""));
    }
}
