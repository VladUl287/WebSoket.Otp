using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface ITokenIdService
{
    string Generate();

    bool TryExclude(HttpContext context, [NotNullWhen(true)] out string? tokenId);
}
