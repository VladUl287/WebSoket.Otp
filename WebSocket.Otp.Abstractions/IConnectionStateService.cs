using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions;

public interface IConnectionStateService
{
    Task<string> GenerateTokenId(HttpContext context, WsConnectionOptions opitons, CancellationToken token = default);
    Task<WsConnectionOptions?> GetAsync(string key, CancellationToken token = default);
    Task RevokeAsync(string key, CancellationToken token = default);
}
