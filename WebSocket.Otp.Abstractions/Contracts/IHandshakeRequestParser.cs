using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandshakeRequestParser
{
    ValueTask<WsConnectionOptions> ParseOptions(HttpRequest request, CancellationToken token);
}
