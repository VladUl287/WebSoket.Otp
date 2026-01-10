using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Services;

public sealed class HandshakeRequestParser(JsonSerializerOptions jsonOptions) : IHandshakeRequestParser
{
    public async ValueTask<WsConnectionOptions> Parse(HttpContext ctx)
    {
        if (!HttpMethods.IsPost(ctx.Request.Method))
            throw new HttpRequestException("Only POST method is supported.");

        if (!ctx.Request.ContentType.Equals(MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase))
            throw new UnsupportedMediaTypeException(
                "Content-Type must be application/json.", MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json));

        var options = await JsonSerializer.DeserializeAsync<WsConnectionOptions>(
            ctx.Request.Body, jsonOptions);

        return options ?? throw new InvalidDataException("Invalid JSON format.");
    }
}
