using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Core.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Contracts;
using System.Runtime.CompilerServices;

namespace WebSockets.Otp.Core.Authorization;

public class WsAuthorizationService : IWsAuthorizationService
{
    private readonly IAuthorizationService _authService;
    private readonly ILogger<WsAuthorizationService> _logger;

    public WsAuthorizationService(IAuthorizationService authorizationService, ILogger<WsAuthorizationService> logger)
    {
        ArgumentNullException.ThrowIfNull(authorizationService, nameof(authorizationService));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _authService = authorizationService;
        _logger = logger;
    }

    public virtual async ValueTask<bool> TryAuhtorize(HttpContext ctx, WsAuthorizationOptions options)
    {
        if (!IsUserAuthenticated(ctx))
            return false;

        if (!IsRolesValid(ctx, options))
            return false;

        if (!IsAuthenticationSchemeValid(ctx, options))
            return false;

        if (!await IsPoliciesValid(ctx, options))
            return false;

        _logger.LogAuthorizationSucceeded();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsUserAuthenticated(HttpContext ctx)
    {
        if (ctx is { User.Identity.IsAuthenticated: false })
        {
            _logger.LogUserNotAuthenticated();
            return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsRolesValid(HttpContext ctx, WsAuthorizationOptions options)
    {
        if (options.Roles is { Length: 0 } || options.Roles.Any(ctx.User.IsInRole))
            return true;

        _logger.LogUserNotInRequiredRole();
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsAuthenticationSchemeValid(HttpContext ctx, WsAuthorizationOptions options)
    {
        if (options is { Schemes.Length: 0 })
            return true;

        var authenticationType = ctx.User.Identity?.AuthenticationType;
        if (authenticationType is null || !options.Schemes.Contains(authenticationType))
            return false;

        _logger.LogInvalidAuthorizationScheme();
        return true;
    }

    private async ValueTask<bool> IsPoliciesValid(HttpContext ctx, WsAuthorizationOptions options)
    {
        if (options is { Policies.Length: 0 })
            return true;

        foreach (var policy in options.Policies)
        {
            var authorizationResult = await _authService.AuthorizeAsync(ctx.User, policy);

            if (!authorizationResult.Succeeded)
            {
                var reasons = authorizationResult.Failure.FailureReasons
                    .Where(c => !string.IsNullOrEmpty(c.Message));
                _logger.LogPolicyAuthorizationFailed(policy, string.Join(' ', reasons));
                return false;
            }
        }

        return true;
    }
}
