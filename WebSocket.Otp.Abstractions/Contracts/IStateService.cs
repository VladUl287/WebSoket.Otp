using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IStateService
{
    ValueTask<WsConnectionOptions?> Get(string key, CancellationToken token = default);
    ValueTask Set(string key, WsConnectionOptions options, CancellationToken token = default);
    ValueTask Delete(string key, CancellationToken token = default);
}
