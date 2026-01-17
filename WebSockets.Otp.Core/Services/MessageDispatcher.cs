using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Pipeline;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Services;

public class MessageDispatcher(
    IServiceScopeFactory scopeFactory, IWsConnectionManager connectionManager, IWsEndpointRegistry endpointRegistry, 
    IExecutionContextFactory contextFactory, IPipelineFactory pipelineFactory, IStringPool stringPool) : IMessageDispatcher
{
    private readonly ReadOnlyMemory<byte> Key = stringPool.Encoding.GetBytes(WsMessageFields.Key).AsMemory();

    public async Task DispatchMessage(
        IGlobalContext globalContext, ISerializer serializer, IMessageBuffer payload, CancellationToken token)
    {
        var endpointKey = serializer.ExtractField(Key.Span, payload.Span, stringPool);

        if (!endpointRegistry.TryResolve(endpointKey, out var endpointType))
            throw new InvalidOperationException($"Endpoint for key '{endpointKey}' not found");

        await using var scope = scopeFactory.CreateAsyncScope();

        var endpoint = scope.ServiceProvider.GetRequiredService(endpointType);

        var execCtx = contextFactory.Create(globalContext, connectionManager, payload, serializer, token);

        var pipeline = pipelineFactory.CreatePipeline(endpointType);

        await pipeline.ExecuteAsync(endpoint, execCtx);
    }
}
