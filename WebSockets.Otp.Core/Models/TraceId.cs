using Microsoft.AspNetCore.Http;

namespace WebSockets.Otp.Core.Models;

public readonly struct TraceId(HttpContext httpContext)
{
    public override string ToString() => httpContext.TraceIdentifier;

    public static implicit operator TraceId(HttpContext ctx) => new(ctx);
}
