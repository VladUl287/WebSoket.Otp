using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Services.Endpoints;

public sealed class EndpointAuthorizer(ILogger<EndpointAuthorizer> logger) : IEndpointAuthorizer
{
    public async Task<Result<ClaimsPrincipal, string>> AuthorizeAsync(
        HttpContext ctx,
        WsEndpointInfo endpointInfo,
        CancellationToken cancellationToken)
    {
        var principal = ctx.User;
        if (principal is null)
            return ctx.User;

        var endpoint = endpointInfo.Endpoint;

        if (endpoint is null)
            return ctx.User;

        var services = ctx.RequestServices;

        var policy = await endpointInfo.GetOrComputePolicy((info, sp) =>
        {
            var endpoint = info.Endpoint;

            if (endpoint is null)
                return Task.FromResult<AuthorizationPolicy?>(null);

            var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            if (authorizeData.Count == 0)
                return Task.FromResult<AuthorizationPolicy?>(null);

            var policyProvider = sp.GetRequiredService<IAuthorizationPolicyProvider>();
            return AuthorizationPolicy.CombineAsync(policyProvider, authorizeData, []);
        }, services);

        if (policy is null)
            return ctx.User;

        var authzService = services.GetRequiredService<IAuthorizationService>();
        var authzResult = await authzService.AuthorizeAsync(principal, resource: null, policy);

        if (!authzResult.Succeeded)
        {
            var failed = authzResult.Failure?.FailedRequirements
                .Select(r => r.ToString() ?? r.GetType().Name)
                .ToArray() ?? [];

            logger.LogDebug("WS endpoint authorization failed. User={User}, Failed={Failed}", principal.Identity?.Name ?? "(anonymous)", string.Join(", ", failed));
            return "authorization failed";
        }

        return principal;
    }
}