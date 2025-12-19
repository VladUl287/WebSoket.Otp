using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface ITokenIdService
{
    string Generate();
    bool TryExclude(HttpRequest request, [NotNullWhen(true)] out string? tokenId);
}
