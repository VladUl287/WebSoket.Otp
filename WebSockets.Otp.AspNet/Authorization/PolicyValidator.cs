using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Results;

namespace WebSockets.Otp.AspNet.Authorization;

public sealed class PolicyValidator : IWsAuthorizationValidator
{
    public async Task<WsAuthorizationResult> ValidateAsync(HttpContext context, WsAuthorizationOptions options)
    {
        if (options is { Policies.Length: > 0 })
        {
            var authorizationService = context.RequestServices.GetService<IAuthorizationService>();
            if (authorizationService is null)
                return WsAuthorizationResult.Failure("");

            foreach (var policy in options.Policies ?? [])
            {
                var policyAuthResult = await authorizationService.AuthorizeAsync(context.User, policy);
                if (policyAuthResult.Failure is not null)
                    return WsAuthorizationResult.Failure("");
            }
        }

        return WsAuthorizationResult.Success();
    }
}
