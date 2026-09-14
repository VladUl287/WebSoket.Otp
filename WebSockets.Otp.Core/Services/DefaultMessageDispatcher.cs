using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Models;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Services;

public class DefaultMessageDispatcher(
    IServiceScopeFactory scopeFactory, IWsConnectionManager connectionManager, IContextFactory contextFactory,
    ITrieResolver<WsEndpointInfo> endpointTypeResolver) : IMessageDispatcher
{
    private readonly ReadOnlyMemory<byte> _endpointKeyBytes = Encoding.UTF8.GetBytes(WsMessageFields.Key).AsMemory();

    public async Task DispatchMessage(IGlobalContext context, ISerializer serializer, IMessageBuffer payload, CancellationToken token)
    {
        var keyIndex = serializer.FieldValueIndex(payload.Span, _endpointKeyBytes.Span);

        if(keyIndex == -1)
        {
            throw new Exception("");
        }

        var endpointInfo = endpointTypeResolver.Resolve(payload.Span.Slice((int)keyIndex));
        if(endpointInfo is null)
        {
            throw new Exception("");
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        var endpointType = endpointInfo.EndpointType;
        var endpoint = scope.ServiceProvider.GetRequiredService(endpointType);

        if(endpointInfo.AuthEndpoint is not null)
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
                return;
            }
        }

        var execCtx = contextFactory.Create(context, connectionManager, payload, serializer, token);

        await endpointInfo.Invoker.Invoke(endpoint, execCtx);
    }
}
