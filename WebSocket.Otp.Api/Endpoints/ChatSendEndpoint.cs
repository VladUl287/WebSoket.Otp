using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Api.Models;

namespace WebSockets.Otp.Api.Endpoints;

[WsEndpoint("chat/message")]
public sealed class ChatSendEndpoint(ILogger<ChatSendEndpoint> log, IWsConnectionManager connectionManager, IMessageSerializer messageSerializer) : WsEndpoint<ChatMessage>
{
    public override async Task HandleAsync(ChatMessage request, IWsContext ctx, CancellationToken token)
    {
        log.LogInformation("Received chat message from {User}: {Text}", request.UserName, request.Content);
        var reply = new ChatMessage("chat/message", request.UserName, request.Content, DateTime.UtcNow);
        var message = messageSerializer.Serialize(reply);
        foreach (var connection in connectionManager.GetAll())
        {
            await connection.SendAsync(message, System.Net.WebSockets.WebSocketMessageType.Text, token);
        }
    }
}
