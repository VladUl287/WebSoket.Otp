using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Results;

namespace WebSockets.Otp.AspNet.Authorization;

public sealed class RoleValidator : IWsAuthorizationValidator
{
    public Task<WsAuthorizationResult> ValidateAsync(HttpContext context, WsAuthorizationOptions options)
    {
        if (options.Roles is { Length: 0 } || options.Roles.Any(context.User.IsInRole))
            return Task.FromResult(WsAuthorizationResult.Success());

        return Task.FromResult(WsAuthorizationResult.Failure(""));
    }
}
