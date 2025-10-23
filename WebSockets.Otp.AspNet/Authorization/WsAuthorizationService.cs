using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Results;

namespace WebSockets.Otp.AspNet.Authorization;

public class WsAuthorizationService(IAuthorizationService authorizationService) : IWsAuthorizationService
{
    public virtual async Task<WsAuthorizationResult> AuhtorizeAsync(HttpContext context, WsAuthorizationOptions options)
    {
        if (options is null or { RequireAuthorization: false })
            return WsAuthorizationResult.Success();

        if (authorizationService is null)
            return WsAuthorizationResult.Failure("Authorization service not presented");

        if (context is { User.Identity.IsAuthenticated: false })
            return WsAuthorizationResult.Failure("User not authenticated");

        if (options is { Policies.Length: > 0 })
        {
            foreach (var policy in options.Policies ?? [])
            {
                var policyAuthResult = await authorizationService.AuthorizeAsync(context.User, policy);
                if (policyAuthResult.Failure is not null)
                {
                    var failureReason = policyAuthResult.Failure.FailureReasons.FirstOrDefault()?.Message ?? $"Policy: '{policy}' unknown authorization failure";
                    return WsAuthorizationResult.Failure(failureReason);
                }
            }
        }

        if (options.Roles is { Length: > 0 } && !options.Roles.Any(context.User.IsInRole))
            return WsAuthorizationResult.Failure("User not in required role");

        var authScheme = context.User.Identity?.AuthenticationType;
        if (options is { Schemes.Length: > 0 } && !options.Schemes.Contains(authScheme))
            return WsAuthorizationResult.Failure("User authorization scheme not correct");

        return WsAuthorizationResult.Success();
    }
}
