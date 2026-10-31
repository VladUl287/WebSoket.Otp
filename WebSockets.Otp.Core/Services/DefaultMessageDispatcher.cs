using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core.Services;

public class DefaultMessageDispatcher(
    IServiceScopeFactory scopeFactory, IContextFactory contextFactory, ITrieResolver<WsEndpointInfo> endpointResolver,
    ILogger<DefaultMessageDispatcher> logger) : IMessageDispatcher
{
    public async Task DispatchMessage(IGlobalContext context, IMessageSerializer serializer, IMessageBuffer payload, CancellationToken token)
    {
        var payloadMem = payload.Memory;

        var messageHeader = serializer.ReadHeaders(payloadMem);

        var keySlice = messageHeader.Key;

        if (keySlice.IsEmpty || !endpointResolver.TryResolve(keySlice, out var endpointInfo))
        {
            logger.FailToResolveFieldInfo(0, payload.Span.Length);
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        if (endpointInfo.Endpoint is not null)
        {
            var authorizer = scope.ServiceProvider.GetRequiredService<IEndpointAuthorizer>();
            var result = await authorizer.AuthorizeAsync(context.Context, endpointInfo);

            if (result.IsError)
            {
                logger.AuthFailed(result.Error);
                return;
            }
        }

        var endpointType = endpointInfo.EndpointType;
        var endpoint = scope.ServiceProvider.GetRequiredService(endpointType);

        var endpointContext = contextFactory.Create(messageHeader.Id, context, messageHeader.Value, token);

        await endpointInfo.Invoker.Invoke(endpoint, endpointContext);
    }
}
