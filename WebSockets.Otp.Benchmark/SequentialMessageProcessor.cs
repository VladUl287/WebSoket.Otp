using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Benchmark.Endpoints;
using WebSockets.Otp.Benchmark.Utils;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Benchmark;

[MemoryDiagnoser]
public class SequentialMessageProcessorBenchmark
{
    public static IServiceProvider CreateServiceProvider()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddWsEndpoints(typeof(BenchmarkEndpoint).Assembly)
            .AddUlidIdProvider();
        builder.Logging.ClearProviders();

        var app = builder.Build();
        return app.Services;
    }

    private IServiceProvider serviceProvider = default!;
    private IMessageProcessor messageProcessor = default!;
    private IWsConnection smallSocketConnection = default!;
    private readonly WsMiddlewareOptions options = new();

    //private static byte[] message = Encoding.UTF8.GetBytes("""
    // { 
    //     "key": "chat/message/singleton",
    // }
    // """);

    private readonly static byte[] Message = Encoding.UTF8.GetBytes("""
     { 
         "key": "chat/message/request"
     }
     """);

    [GlobalSetup]
    public void Setup()
    {
        serviceProvider = CreateServiceProvider();
        var factory = serviceProvider.GetRequiredService<IMessageProcessorFactory>();
        messageProcessor = factory.Create(ProcessingMode.Sequential);

        var registry = serviceProvider.GetRequiredService<IWsEndpointRegistry>();
        //var enpointsTypes = new Assembly[] { typeof(BenchmarkEndpoint).Assembly }.GetEndpoints();
        //registry.Register(enpointsTypes);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        var webSocket = new MockWebSocket(Message, 100_000);

        var cancellationTokenSource = new CancellationTokenSource();
        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.RequestAborted)
            .Returns(cancellationTokenSource.Token);

        smallSocketConnection = new WsConnection(Guid.NewGuid().ToString(), mockHttpContext.Object, webSocket);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        (serviceProvider as IDisposable)?.Dispose();
    }

    [Benchmark]
    public async Task BenchmarkMethod()
    {
        //await messageProcessor.Process(smallSocketConnection, options);
    }
}

