using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Logging;
using WebSockets.Otp.Core.Models;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Services;

public class DefaultMessageDispatcher(
    IServiceScopeFactory scopeFactory, IWsConnectionManager connectionManager, IContextFactory contextFactory,
    ITrieResolver<WsEndpointInfo> endpointTypeResolver, ILogger<DefaultMessageDispatcher> logger) : IMessageDispatcher
{
    public async Task DispatchMessage(IGlobalContext context, ISerializer serializer, IMessageBuffer payload, CancellationToken token)
    {
        if (!serializer.TryGetFieldValueIndex(payload.Span, WsMessageFields.Key, out var keyIndex))
        {
            logger.MessageKeyFieldMissing();
            return;
        }

        if (!endpointTypeResolver.TryResolve(payload.Span[keyIndex..], out var endpointInfo))
        {
            logger.FailToResolveFieldInfo(keyIndex, payload.Span.Length);
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        if (endpointInfo.AuthEndpoint is not null)
        {
            var source = context.Context;
            var ctx = new DefaultHttpContext
            {
                RequestServices = scope.ServiceProvider,
                User = source.User,
                RequestAborted = token,
                Items = source.Items
            };

            ctx.SetEndpoint(endpointInfo.AuthEndpoint);

            await context.Options.AuthPipeline(ctx);

            if (ctx.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
            {
                logger.AuthFailed(ctx.Response.StatusCode);
                return;
            }
        }

        var endpointType = endpointInfo.EndpointType;
        var endpoint = scope.ServiceProvider.GetRequiredService(endpointType);

        var execCtx = contextFactory.Create(context, connectionManager, payload, serializer, token);
        await endpointInfo.Invoker.Invoke(endpoint, execCtx);
    }
}
