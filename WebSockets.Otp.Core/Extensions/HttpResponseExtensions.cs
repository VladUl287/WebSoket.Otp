using Microsoft.AspNetCore.Http;
using System.Buffers;
using System.Text;

namespace WebSockets.Otp.Core.Extensions;

public static class HttpResponseExtensions
{
    public static ValueTask WriteAsync(this HttpResponse response, int statusCode, string message, CancellationToken token) => 
        WriteAsync(response, Encoding.UTF8, statusCode, message, token);

    private static async ValueTask WriteAsync(HttpResponse response, Encoding encoding, int statusCode, string message, CancellationToken token)
    {
        response.StatusCode = statusCode;

        var maximumBytesCount = encoding.GetMaxByteCount(message.Length);
        var bytes = ArrayPool<byte>.Shared.Rent(maximumBytesCount);
        var count = encoding.GetBytes(message, bytes);

        await response.Body.WriteAsync(bytes.AsMemory(0, count), token);

        ArrayPool<byte>.Shared.Return(bytes);
    }
}
