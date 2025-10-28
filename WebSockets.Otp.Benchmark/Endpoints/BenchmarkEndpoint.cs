using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Benchmark.Endpoints;

[WebSockets.Otp.Abstractions.Attributes.WsEndpoint("chat/message/singleton", ServiceLifetime.Singleton)]
public class BenchmarkEndpoint : WebSockets.Otp.Abstractions.WsEndpoint
{
    public override Task HandleAsync(IWsExecutionContext connection, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}
