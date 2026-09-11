using Microsoft.Extensions.DependencyInjection;
using System.Text;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Services;

public class DefaultMessageDispatcher(
    IServiceScopeFactory scopeFactory, IWsConnectionManager connectionManager, IContextFactory contextFactory,
    IEndpointInvoker invoker, ITrieResolver endpointTypeResolver) : IMessageDispatcher
{
    private readonly ReadOnlyMemory<byte> _endpointKeyBytes = Encoding.UTF8.GetBytes(WsMessageFields.Key).AsMemory();

    public async Task DispatchMessage(
        IGlobalContext globalContext, ISerializer serializer, IMessageBuffer payload, CancellationToken token)
    {
        var keyIndex = serializer.FieldIndex(payload.Span, _endpointKeyBytes.Span);

        if(keyIndex == -1)
        {
            throw new Exception("");
        }

        var endpointType = endpointTypeResolver.Resolve(payload.Span.Slice((int)keyIndex));
        if(endpointType is null)
        {
            throw new Exception("");
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        var endpoint = scope.ServiceProvider.GetRequiredService(endpointType);

        var execCtx = contextFactory.Create(globalContext, connectionManager, payload, serializer, token);

        await invoker.Invoke(endpoint, execCtx);
    }
}
