using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Results;
using WebSockets.Otp.AspNet.Logging;

namespace WebSockets.Otp.AspNet.Authorization;

public class WsAuthorizationService(IAuthorizationService authorizationService, ILogger<WsAuthorizationService> logger) : IWsAuthorizationService
{
    public virtual async Task<WsAuthorizationResult> AuhtorizeAsync(HttpContext context, Abstractions.Options.AuthorizationSettings options)
    {
        if (options is null or { RequireAuthorization: false })
        {
            logger.LogAuthorizationNotRequired();
            return WsAuthorizationResult.Success();
        }

        if (authorizationService is null)
        {
            logger.LogAuthorizationServiceMissing();
            return WsAuthorizationResult.Failure("Authorization service not provided");
        }

        if (context is { User.Identity.IsAuthenticated: false })
        {
            logger.LogUserNotAuthenticated();
            return WsAuthorizationResult.Failure("User is not authenticated");
        }

        if (options is { Policies.Length: > 0 })
        {
            foreach (var policy in options.Policies ?? [])
            {
                var policyAuthResult = await authorizationService.AuthorizeAsync(context.User, policy);
                if (policyAuthResult.Failure is not null)
                {
                    var failureReason = policyAuthResult.Failure.FailureReasons.FirstOrDefault()?.Message ?? $"Policy '{policy}': unknown authorization failure";
                    logger.LogPolicyAuthorizationFailed(policy, failureReason);
                    return WsAuthorizationResult.Failure(failureReason);
                }
            }
        }

        if (options.Roles is { Length: > 0 } && !options.Roles.Any(context.User.IsInRole))
        {
            logger.LogUserNotInRequiredRole();
            return WsAuthorizationResult.Failure("User is not in the required role");
        }

        var authScheme = context.User.Identity?.AuthenticationType;
        if (options is { Schemes.Length: > 0 } && !options.Schemes.Contains(authScheme))
        {
            logger.LogInvalidAuthorizationScheme();
            return WsAuthorizationResult.Failure("User authorization scheme is not valid");
        }

        logger.LogAuthorizationSucceeded();
        return WsAuthorizationResult.Success();
    }
}
