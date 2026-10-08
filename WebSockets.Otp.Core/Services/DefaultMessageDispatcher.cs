using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Buffers;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Logging;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Services;

public class DefaultMessageDispatcher(
    IServiceScopeFactory scopeFactory, IContextFactory contextFactory, ITrieResolver<WsEndpointInfo> endpointTypeResolver,
    ILogger<DefaultMessageDispatcher> logger) : IMessageDispatcher
{
    public async Task DispatchMessage(IGlobalContext context, IMessageSerializer serializer, IMessageBuffer payload, CancellationToken token)
    {
        var results = ArrayPool<JsonSlice>.Shared.Rent(16);

        JsonSlice keySlice;
        JsonSlice correlationSlice;
        JsonSlice valueSlice;

        try
        {
            serializer.ScanMessage(payload.Span, results);

            keySlice = results[0];
            correlationSlice = results[1];
            valueSlice = results[2];
        }
        finally
        {
            ArrayPool<JsonSlice>.Shared.Return(results);
        }

        if (!keySlice.Found || !endpointTypeResolver.TryResolve(payload.Span[(keySlice.Start + 1)..], out var endpointInfo))
        {
            logger.FailToResolveFieldInfo(keySlice.Start, payload.Span.Length);
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        EndpointAuthResult? authResult = null;
        if (endpointInfo.AuthEndpoint is not null)
        {
            var authorizer = context.Context.RequestServices.GetRequiredService<IEndpointAuthorizer>();
            authResult = await authorizer.AuthorizeAsync(context.Context, endpointInfo.AuthEndpoint, token);

            if (!authResult.Succeeded)
            {
                logger.AuthFailed(authResult.FailureReason ?? "authorization failed");
                return;
            }
        }

        var endpointType = endpointInfo.EndpointType;
        var endpoint = scope.ServiceProvider.GetRequiredService(endpointType);

        var correlationId = 0u;
        if (correlationSlice.Found)
        {
            correlationId = serializer.Deserialize<uint>(payload.Span[correlationSlice.Start..correlationSlice.End]);
        }

        var data = payload.Memory[valueSlice.Start..valueSlice.End];

        var headers = new EndpointHeaders() { Key = endpointInfo.Key, CorrelationId = correlationId };
        var execCtx = contextFactory.Create(headers, context, data, serializer, authResult.User, token);

        await endpointInfo.Invoker.Invoke(endpoint, execCtx);
    }
}
