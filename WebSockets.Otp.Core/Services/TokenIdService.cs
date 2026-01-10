using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Services;

public sealed class TokenIdService : ITokenIdService
{
    public string Generate() => Guid.CreateVersion7().ToString("N");

    public bool TryExclude(HttpContext ctx, [NotNullWhen(true)] out string? tokenId)
    {
        const string QueryKey = "";

        if(ctx.Request.Query.TryGetValue(QueryKey, out var values) && values is { Count: > 0 })
        {
            tokenId = values.ToString();
            return true;
        }

        tokenId = null;
        return false;
    }
}
