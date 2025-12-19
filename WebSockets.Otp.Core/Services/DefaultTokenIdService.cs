using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Services;

public sealed class DefaultTokenIdService(string queryKey) : ITokenIdService
{
    public string Generate() => Guid.CreateVersion7().ToString("N");

    public bool TryExclude(HttpRequest request, [NotNullWhen(true)] out string? tokenId)
    {
        if(request.Query.TryGetValue(queryKey, out var values) && values is { Count: > 0 })
        {
            tokenId = values.ToString();
            return true;
        }

        tokenId = null;
        return false;
    }
}
