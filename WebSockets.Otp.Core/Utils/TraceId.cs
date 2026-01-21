using Microsoft.AspNetCore.Http;

namespace WebSockets.Otp.Core.Utils;

public readonly struct TraceId(HttpContext context)
{
    public override string ToString() => context.TraceIdentifier;

    public static implicit operator TraceId(HttpContext ctx) => new(ctx);
}
