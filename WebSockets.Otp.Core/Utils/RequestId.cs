using Microsoft.AspNetCore.Http;

namespace WebSockets.Otp.Core.Utils;

public readonly struct RequestId(HttpContext context)
{
    public override string ToString() => context.TraceIdentifier;

    public static implicit operator RequestId(HttpContext ctx) => new(ctx);
}
