using BenchmarkDotNet.Attributes;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Core.Models;
using WebSockets.Otp.Core.Services.Endpoints;

namespace WebSockets.Otp.Benchmark.EndpointInvokers;

[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 10)]
public class EmptyEndpointInvokerBench
{
    public IEndpointInvoker GenericEndpointInvoker = new EmptyEndpointInvoker();

    private readonly EndpointTest EndpointTestInstance = new();
    private readonly WsEndpointContext EndpointContext = default!;

    public EmptyEndpointInvokerBench()
    {
        EndpointContext = new WsEndpointContext(new WsGlobalContext(default!, default!, default!, default!), default!, default!, default!, default);
    }

    [Benchmark]
    public Task Invoke_Direct() => EndpointTestInstance.HandleAsync(EndpointContext);

    [Benchmark]
    public Task Invoke_Generic() => GenericEndpointInvoker.Invoke(EndpointTestInstance, EndpointContext);

    public sealed class EndpointTest : WsEndpoint
    {
        public override Task HandleAsync(EndpointContext context)
        {
            return Task.CompletedTask;
        }
    }
}
