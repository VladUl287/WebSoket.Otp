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
        log.LogInformation("Received chat message: {Text}", request.Content);
        var reply = new ChatMessage
        {
            Content = request.Content,
        };
        var message = messageSerializer.Serialize(reply);
        foreach (var connection in connectionManager.GetAll())
        {
            await connectionManager.SendAsync(connection.Id, message, token);
        }
    }
}
