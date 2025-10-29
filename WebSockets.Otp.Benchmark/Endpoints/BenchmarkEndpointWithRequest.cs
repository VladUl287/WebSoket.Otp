using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Benchmark.Endpoints;

[WebSockets.Otp.Abstractions.Attributes.WsEndpoint("chat/message/request", ServiceLifetime.Singleton)]
public sealed class BenchmarkEndpointWithRequest : WebSockets.Otp.Abstractions.WsEndpoint<Request>
{
    public override Task HandleAsync(Request request, IWsExecutionContext context, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}

public sealed class Request : WsMessage;
