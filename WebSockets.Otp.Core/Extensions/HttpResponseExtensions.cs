using Microsoft.AspNetCore.Http;
using System.Buffers;
using System.Text;

namespace WebSockets.Otp.Core.Extensions;

public static class HttpResponseExtensions
{
    public static ValueTask WriteAsync(this HttpContext ctx, int statusCode, string message, CancellationToken token) =>
        WriteAsync(ctx, Encoding.UTF8, statusCode, message, token);

    private static async ValueTask WriteAsync(HttpContext ctx, Encoding encoding, int statusCode, string message, CancellationToken token)
    {
        ctx.Response.StatusCode = statusCode;

        var maximumBytesCount = encoding.GetMaxByteCount(message.Length);
        var bytes = ArrayPool<byte>.Shared.Rent(maximumBytesCount);
        var count = encoding.GetBytes(message, bytes);

        await ctx.Response.Body.WriteAsync(bytes.AsMemory(0, count), token);

        ArrayPool<byte>.Shared.Return(bytes);
    }
}
