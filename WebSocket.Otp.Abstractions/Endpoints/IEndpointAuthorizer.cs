using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace WebSockets.Otp.Abstractions.Endpoints;

public sealed record EndpointAuthResult(bool Succeeded, ClaimsPrincipal? User, string? FailureReason, IReadOnlyList<string> FailedRequirements);

public interface IEndpointAuthorizer
{
    Task<EndpointAuthResult> AuthorizeAsync(HttpContext context, Endpoint endpoint, CancellationToken token);
}
