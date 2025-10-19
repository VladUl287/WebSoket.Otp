using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.AspNet;

public sealed class WsAuthorizationService : IWsAuthorizationService
{
    public async Task<bool> Auhtorize(HttpContext context, WsAuthorizationOptions options)
    {
        if (context is { User.Identity.IsAuthenticated: false })
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return false;
        }

        var customAuthResult = options is { CustomValidation: not null } && await options.CustomValidation.Invoke(context);
        if (!customAuthResult)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return false;
        }

        var authorizationService = context.RequestServices.GetRequiredService<IAuthorizationService>();

        foreach (var policy in options.Policies ?? [])
        {
            var policyAuthResult = await authorizationService.AuthorizeAsync(context.User, policy);
            if (policyAuthResult.Failure is not null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return false;
            }
        }

        var roleAuthResult = options.Roles is { Length: > 0 } && options.Roles.Any(context.User.IsInRole);
        if (!roleAuthResult)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return false;
        }

        if (options is { Schemes.Length: > 0 })
        {
            var authScheme = context.User.Identity?.AuthenticationType;
            if (!options.Schemes.Contains(authScheme))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return false;
            }
        }

        return true;
    }
}
