using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Pipeline;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Services;

public class MessageDispatcher(
    IServiceScopeFactory scopeFactory, IWsConnectionManager connectionManager, IContextFactory contextFactory,
    IPipelineFactory pipelineFactory, IStringPool stringPool) : IMessageDispatcher
{
    private readonly ReadOnlyMemory<byte> _endpointKeyBytes = stringPool.Encoding.GetBytes(WsMessageFields.Key).AsMemory();

    public async Task DispatchMessage(
        IGlobalContext globalContext, ISerializer serializer, IMessageBuffer payload, CancellationToken token)
    {
        var endpointKey = serializer.ExtractField(
            _endpointKeyBytes.Span,
            payload.Span,
            stringPool
        );

        await using var scope = scopeFactory.CreateAsyncScope();

        var endpoint = scope.ServiceProvider.GetRequiredKeyedService(typeof(IWsEndpoint), endpointKey);

        var execCtx = contextFactory.Create(
            globalContext,
            connectionManager,
            payload,
            serializer,
            token);

        var endpointType = endpoint.GetType();
        var pipeline = pipelineFactory.CreatePipeline(endpointType);

        await pipeline.ExecuteAsync(endpoint, execCtx);
    }
}
