using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Abstractions.Endpoints;

public sealed record EndpointAuthResult(bool Succeeded, ClaimsPrincipal? User, string? FailureReason, IReadOnlyList<string> FailedRequirements);

public interface IEndpointAuthorizer
{
    Task<Result<ClaimsPrincipal, string>> AuthorizeAsync(HttpContext context, Endpoint endpoint, CancellationToken token);
}
