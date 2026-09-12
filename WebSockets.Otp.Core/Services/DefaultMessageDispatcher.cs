using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;
using System.Text;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Services;

public class DefaultMessageDispatcher(
    IServiceScopeFactory scopeFactory, IWsConnectionManager connectionManager, IContextFactory contextFactory,
    IEndpointInvokerFactory invokerFactory, ITrieResolver endpointTypeResolver) : IMessageDispatcher
{
    private readonly ReadOnlyMemory<byte> _endpointKeyBytes = Encoding.UTF8.GetBytes(WsMessageFields.Key).AsMemory();

    public async Task DispatchMessage(
        IGlobalContext globalContext, ISerializer serializer, IMessageBuffer payload, WsConfiguration configuration, CancellationToken token)
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

        var source = globalContext.Context;
        var ctx = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            User = source.User,
            RequestAborted = token,
            Items = source.Items
        };

        var attribute = endpointType.GetCustomAttribute<AuthorizeAttribute>() ??
              throw new InvalidOperationException($"Type {endpointType.Name} is missing WsEndpointAttribute");
        
        ctx.SetEndpoint(new Endpoint(
            requestDelegate: null,
            metadata: new EndpointMetadataCollection(attribute),
            displayName: "ws-auth"));

        await configuration.AuthPipeline(ctx);

        if (ctx.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            return;
        }

        var invoker = invokerFactory.Create(endpointType);
        await invoker.Invoke(endpoint, execCtx);
    }
}
