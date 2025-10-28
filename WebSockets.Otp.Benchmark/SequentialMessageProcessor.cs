using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Moq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Benchmark.Endpoints;
using WebSockets.Otp.Core;
using WebSockets.Otp.Core.Extensions;

namespace WebSockets.Otp.Benchmark;

[MemoryDiagnoser]
public class SequentialMessageProcessorBenchmark
{
    public static IServiceProvider CreateServiceProvider()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddWsEndpoints(typeof(BenchmarkEndpoint).Assembly);
        builder.Logging.ClearProviders();

        var app = builder.Build();
        return app.Services;
    }

    private IServiceProvider serviceProvider = default!;
    private IMessageProcessor messageProcessor = default!;
    private IWsConnection smallSocketConnection = default!;
    private readonly WsMiddlewareOptions options = new();

    [GlobalSetup]
    public void Setup()
    {
        serviceProvider = CreateServiceProvider();
        var factory = serviceProvider.GetRequiredService<IMessageProcessorFactory>();
        messageProcessor = factory.Create(ProcessingMode.Sequential);

        var registry = serviceProvider.GetRequiredService<IWsEndpointRegistry>();
        var enpointsTypes = new Assembly[] { typeof(BenchmarkEndpoint).Assembly }.GetEndpoints();
        registry.Register(enpointsTypes);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        var webSocket = new MockWebSocket();

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
        await messageProcessor.Process(smallSocketConnection, options);
    }
}

public sealed class AsyncServiceScopePooledObjectPolicy : IPooledObjectPolicy<IServiceScope>
{
    private readonly IServiceProvider _serviceProvider;

    public AsyncServiceScopePooledObjectPolicy(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IServiceScope Create()
    {
        return _serviceProvider.CreateScope();
    }

    public bool Return(IServiceScope obj)
    {
        if (obj is AsyncServiceScope asyncScpo)
        {

        }
        return true;
    }
}

public class MockWebSocket : WebSocket
{
    private WebSocketState _state = WebSocketState.Open;

    public override WebSocketCloseStatus? CloseStatus => WebSocketCloseStatus.NormalClosure;
    public override string CloseStatusDescription => "Normal closure";
    public override WebSocketState State => _state;
    public override string SubProtocol => string.Empty;

    private static int Readed = 0;

    private static byte[] message = Encoding.UTF8.GetBytes("""
     { 
         "key": "chat/message/singleton", 
         "chatId": "deace9c3-5984-4241-8e65-927dc4ded8bf", 
         "timestamp": "2011-10-05T14:48:00.000Z", 
         "content": "test" 
     }
     """);

    private static readonly WebSocketReceiveResult defautlResult = new WebSocketReceiveResult(
            message.Length,
            WebSocketMessageType.Text,
            true);

    public override async Task<WebSocketReceiveResult> ReceiveAsync(
        ArraySegment<byte> buffer,
        CancellationToken cancellationToken)
    {
        if (Readed > 100_000)
        {
            Readed = 0;
            return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
        }

        var bytesToCopy = Math.Min(message.Length, buffer.Count);
        Array.Copy(message, 0, buffer.Array, buffer.Offset, bytesToCopy);

        Readed++;
        return defautlResult;
    }

    public override Task SendAsync(
        ArraySegment<byte> buffer,
        WebSocketMessageType messageType,
        bool endOfMessage,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override Task CloseAsync(
        WebSocketCloseStatus closeStatus,
        string statusDescription,
        CancellationToken cancellationToken)
    {
        _state = WebSocketState.Closed;
        return Task.CompletedTask;
    }

    public override Task CloseOutputAsync(
        WebSocketCloseStatus closeStatus,
        string statusDescription,
        CancellationToken cancellationToken)
    {
        _state = WebSocketState.Closed;
        return Task.CompletedTask;
    }

    public override void Abort()
    {
        _state = WebSocketState.Closed;
    }

    public override void Dispose()
    {
        _state = WebSocketState.Closed;
    }
}