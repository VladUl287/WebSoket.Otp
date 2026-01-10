using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Services;

public sealed class HandshakeRequestParser(JsonSerializerOptions jsonOptions) : IHandshakeRequestParser
{
    public ValueTask<WsConnectionOptions?> Deserialize(HttpContext ctx)
    {
        if (!HttpMethods.IsPost(ctx.Request.Method))
            throw new HttpRequestException("Only POST method is supported.");

        if (!ctx.Request.ContentType.Equals(MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase))
            throw new UnsupportedMediaTypeException(
                "Content-Type must be application/json.", MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json));

        return JsonSerializer.DeserializeAsync<WsConnectionOptions>(
            ctx.Request.Body, jsonOptions);
    }
}
