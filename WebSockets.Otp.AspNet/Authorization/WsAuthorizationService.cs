using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Results;

namespace WebSockets.Otp.AspNet.Authorization;

public sealed class WsAuthorizationService(IEnumerable<IWsAuthorizationValidator> validators) : IWsAuthorizationService
{
    private readonly ConcurrentDictionary<string, string> _connectionTokens = [];

    public async Task<WsAuthorizationResult> AuhtorizeAsync(HttpContext context, WsAuthorizationOptions options)
    {
        if (options is null or { RequireAuthorization: false })
            return WsAuthorizationResult.Success();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, options);
            if (result.Failed)
                return result;
        }

        return WsAuthorizationResult.Success();
    }

    public string GenerateConnectionToken(string userId)
    {
        var token = Guid.NewGuid().ToString();
        _connectionTokens[token] = userId;
        return token;
    }

    public void RemoveConnectionToken(string connectionToken)
    {
        _connectionTokens.TryRemove(connectionToken, out _);
    }

    public (bool isValid, string? userId) ValidateConnectionToken(string connectionToken)
    {
        if (_connectionTokens.TryGetValue(connectionToken, out var userId))
            return (true, userId);

        return (false, null);
    }
}
