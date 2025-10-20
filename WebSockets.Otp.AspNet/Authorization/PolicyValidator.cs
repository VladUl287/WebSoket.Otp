using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Results;

namespace WebSockets.Otp.AspNet.Authorization;

public sealed class PolicyValidator(IAuthorizationService authorizationService) : IWsAuthorizationValidator
{
    public async Task<AuthValidationResult> ValidateAsync(HttpContext context, WsAuthorizationOptions options)
    {
        foreach (var policy in options.Policies ?? [])
        {
            var policyAuthResult = await authorizationService.AuthorizeAsync(context.User, policy);
            if (policyAuthResult.Failure is not null)
                return AuthValidationResult.Failure(StatusCodes.Status403Forbidden, "");
        }

        return AuthValidationResult.Success();
    }
}
