using System.Net.Mime;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Services;

public sealed class DefaultJsonHandshakeRequestParser(JsonSerializerOptions jsonOptions) : IHandshakeRequestParser
{
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="JsonException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    public async ValueTask<WsConnectionOptions> ParseOptions(HttpRequest request, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        token.ThrowIfCancellationRequested();

        if (!HttpMethods.IsPost(request.Method))
            throw new HttpRequestException("Only POST method is supported.");

        if (!request.ContentType.Equals(MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase))
            throw new UnsupportedMediaTypeException(
                "Content-Type must be application/json.", MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json));

        token.ThrowIfCancellationRequested();

        var options = await JsonSerializer.DeserializeAsync<WsConnectionOptions>(
            request.Body, jsonOptions, token);

        return options ?? throw new InvalidDataException("Invalid JSON format.");
    }
}
