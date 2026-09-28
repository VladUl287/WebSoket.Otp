using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Services.Endpoints;

public sealed class EndpointAuthorizer(ILogger<EndpointAuthorizer> logger) : IEndpointAuthorizer
{
    public async Task<EndpointAuthResult> AuthorizeAsync(
        HttpContext sourceCtx,
        Endpoint endpoint,
        CancellationToken cancellationToken)
    {
        var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();

        if (authorizeData.Count == 0)
        {
            return new EndpointAuthResult(true, sourceCtx.User, null, []);
        }

        var ctx = CopyContext(sourceCtx, cancellationToken);
        ctx.SetEndpoint(endpoint);

        var services = ctx.RequestServices;
        var authService = services.GetRequiredService<IAuthenticationService>();
        var authzService = services.GetRequiredService<IAuthorizationService>();
        var policyProvider = services.GetRequiredService<IAuthorizationPolicyProvider>();
        var authOptions = services.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        var policy = await AuthorizationPolicy.CombineAsync(policyProvider, authorizeData, []);

        if (policy is null)
        {
            return new EndpointAuthResult(true, sourceCtx.User, null, []);
        }

        var schemes = policy.AuthenticationSchemes.Count > 0
            ? policy.AuthenticationSchemes
            : [.. authorizeData
                .SelectMany(d => (d.AuthenticationSchemes ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.Ordinal)];

        ClaimsPrincipal? principal = null;
        if (schemes.Count > 0)
        {
            AuthenticateResult? result = null;

            foreach (var scheme in schemes)
            {
                result = await authService.AuthenticateAsync(ctx, scheme);
                if (result.Succeeded)
                {
                    principal = result.Principal;
                    break;
                }
            }

            if (principal is null)
            {
                var reason = result?.Failure?.Message ?? "no scheme produced a principal";
                logger.LogDebug("WS endpoint auth failed: no scheme succeeded. Schemes={Schemes}, Reason={Reason}", string.Join(",", schemes), reason);
                return new EndpointAuthResult(false, sourceCtx.User, reason, []);
            }
        }
        else
        {
            var defaultScheme = authOptions.DefaultAuthenticateScheme ?? authOptions.DefaultScheme;
            if (defaultScheme is not null)
            {
                var result = await authService.AuthenticateAsync(ctx, defaultScheme);
                if (result.Succeeded)
                {
                    principal = result.Principal;
                }
            }

            principal ??= sourceCtx.User;
        }

        if (principal?.Identity?.IsAuthenticated != true)
        {
            logger.LogDebug("WS endpoint auth failed: principal not authenticated.");
            return new EndpointAuthResult(false, principal, "principal not authenticated", Array.Empty<string>());
        }

        var authzResult = await authzService.AuthorizeAsync(principal, resource: null, policy);

        if (!authzResult.Succeeded)
        {
            var failed = authzResult.Failure?.FailedRequirements
                .Select(r => r.ToString() ?? r.GetType().Name)
                .ToArray() ?? [];

            logger.LogDebug("WS endpoint authorization failed. User={User}, Failed={Failed}", principal.Identity?.Name ?? "(anonymous)", string.Join(", ", failed));
            return new EndpointAuthResult(false, principal, "authorization failed", failed);
        }

        return new EndpointAuthResult(true, principal, null, []);
    }

    private static DefaultHttpContext CopyContext(HttpContext source, CancellationToken token)
    {
        var ctx = new DefaultHttpContext
        {
            RequestServices = source.RequestServices,
            RequestAborted = token,
            TraceIdentifier = source.TraceIdentifier,
        };

        var srcReq = source.Request;
        var dstReq = ctx.Request;

        dstReq.Method = srcReq.Method;
        dstReq.Scheme = srcReq.Scheme;
        dstReq.Host = srcReq.Host;
        dstReq.Path = srcReq.Path;
        dstReq.PathBase = srcReq.PathBase;
        dstReq.QueryString = srcReq.QueryString;
        dstReq.Protocol = srcReq.Protocol;
        dstReq.ContentType = srcReq.ContentType;
        dstReq.ContentLength = srcReq.ContentLength;

        foreach (var h in srcReq.Headers)
        {
            dstReq.Headers[h.Key] = h.Value;
        }

        var srcConn = source.Connection;
        var dstConn = ctx.Connection;
        dstConn.RemoteIpAddress = srcConn.RemoteIpAddress;
        dstConn.RemotePort = srcConn.RemotePort;
        dstConn.LocalIpAddress = srcConn.LocalIpAddress;
        dstConn.LocalPort = srcConn.LocalPort;
        dstConn.Id = srcConn.Id;

        ctx.User = source.User;

        return ctx;
    }
}