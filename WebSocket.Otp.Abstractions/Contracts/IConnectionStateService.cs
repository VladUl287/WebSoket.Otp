using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IConnectionStateService
{
    ValueTask<string> GenerateTokenId(HttpContext context, WsConnectionOptions opitons, CancellationToken token = default);
    ValueTask<WsConnectionOptions?> GetAsync(string key, CancellationToken token = default);
    ValueTask RevokeAsync(string key, CancellationToken token = default);
}
