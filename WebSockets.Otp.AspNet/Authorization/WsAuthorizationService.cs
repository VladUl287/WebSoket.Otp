using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Results;

namespace WebSockets.Otp.AspNet.Authorization;

public sealed class WsAuthorizationService(IWsAuthorizationValidator[] validators) : IWsAuthorizationService
{
    public async Task<AuthValidationResult> AuhtorizeAsync(HttpContext context, WsAuthorizationOptions options)
    {
        if (options is null or { RequireAuthorization: false })
            return AuthValidationResult.Success();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, options);
            if (result.Failed)
                return result;
        }

        return AuthValidationResult.Success();
    }
}
