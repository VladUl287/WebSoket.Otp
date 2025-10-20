using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Api.Models;

namespace WebSockets.Otp.Api.Endpoints;

[WsEndpoint("chat/message")]
public sealed class ChatSendEndpoint(IWsConnectionManager connectionManager) : WsEndpoint<ChatMessage>
{
    public override async Task HandleAsync(ChatMessage request, IWsExecutionContext ctx, CancellationToken token)
    {
        var message = ctx.Serializer.Serialize(
            new ChatMessage
            {
                Content = request.Content,
            });
        await connectionManager.SendAsync(connectionManager.EnumerateIds(), message, token);
    }
}
