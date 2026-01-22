using BenchmarkDotNet.Attributes;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Models;
using WebSockets.Otp.Core.Services.Endpoints;
using WebSockets.Otp.Core.Services.Endpoints.Generic;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Benchmark;

[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 10)]
public class EndpointInvokerBench
{
    public IEndpointInvoker ReflectionBasedInvoker = new ReflectionEndpointInvoker(typeof(RequestEndpointTest));
    public IEndpointInvoker GenericEndpointInvoker = new RequestEndpointInvoker<Message>();

    private readonly RequestEndpointTest RequestEndpointTest = new RequestEndpointTest();
    private readonly object Endpoint = new RequestEndpointTest();
    private WsEndpointContext WsEndpointContext = default!;
    private IEndpointContext EndpointContext = default!;
    private IMessageBuffer Payload = default!;
    private static readonly ISerializer Serializer = new MockSerializer();

    [GlobalSetup]
    public void Setup()
    {
        Payload = new NativeChunkedBuffer(0);
        WsEndpointContext = new WsEndpointContext(new WsGlobalContext(default!, default!, default!, default!), default!, Serializer, Payload, default);
        EndpointContext = WsEndpointContext;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Payload.Dispose();
    }

    [Benchmark]
    public Task Invoke_Direct()
    {
        var message = Serializer.Deserialize(typeof(Message), Payload.Span);
        return RequestEndpointTest.HandleAsync((Message)message, WsEndpointContext);
    }

    [Benchmark]
    public Task Invoke_Reflection() => ReflectionBasedInvoker.Invoke(Endpoint, EndpointContext);

    [Benchmark]
    public Task Invoke_Generic() => GenericEndpointInvoker.Invoke(Endpoint, EndpointContext);
}

public sealed class MockSerializer : ISerializer
{
    public string ProtocolName => throw new NotImplementedException();

    public static readonly Message Message = new()
    {
        Id = Guid.NewGuid(),
        Content = "empty",
        Timestamp = DateTime.UtcNow
    };

    public static readonly object MessageObj = Message;

    public object? Deserialize(Type type, ReadOnlySpan<byte> data) => Message;

    public string ExtractField(ReadOnlySpan<byte> field, ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public string ExtractField(ReadOnlySpan<byte> field, ReadOnlySpan<byte> data, IStringPool stringPool)
    {
        throw new NotImplementedException();
    }

    public ReadOnlyMemory<byte> Serialize<T>(T message)
    {
        throw new NotImplementedException();
    }
}

public sealed class RequestEndpointTest : WsEndpoint<Message>
{
    public override Task HandleAsync(Message request, EndpointContext context)
    {
        return Task.CompletedTask;
    }
}

public sealed class Message
{
    public Guid Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}