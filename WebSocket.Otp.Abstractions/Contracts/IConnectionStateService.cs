using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IConnectionStateService
{
    ValueTask<string> GenerateTokenId(HttpContext context, ConnectionSettings opitons, CancellationToken token = default);
    ValueTask<ConnectionSettings?> GetAsync(string key, CancellationToken token = default);
    ValueTask RevokeAsync(string key, CancellationToken token = default);
}
