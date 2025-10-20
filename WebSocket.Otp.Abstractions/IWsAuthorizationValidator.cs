using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Results;

namespace WebSockets.Otp.Abstractions;

public interface IWsAuthorizationValidator
{
    public Task<WsAuthorizationResult> ValidateAsync(HttpContext context, WsAuthorizationOptions options);
}