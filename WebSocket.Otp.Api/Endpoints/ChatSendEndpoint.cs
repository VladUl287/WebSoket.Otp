using Microsoft.EntityFrameworkCore;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Api.Database;
using WebSockets.Otp.Api.Database.Models;
using WebSockets.Otp.Api.Models;
using WebSockets.Otp.Api.Services.Contracts;

namespace WebSockets.Otp.Api.Endpoints;

[WsEndpoint("chat/message/send")]
public sealed class ChatSendEndpoint(IWsConnectionManager connectionManager, IStorage<long> storage, DatabaseContext dbContext) : WsEndpoint<ChatMessage>
{
    public override async Task HandleAsync(ChatMessage request, IWsExecutionContext ctx, CancellationToken token)
    {
        var userId = ctx.Connection.Context.User.GetUserId<long>();

        var userInChat = await dbContext.ChatsUsers.AnyAsync(c => c.UserId == userId && c.ChatId == request.ChatId, token);
        if (!userInChat)
            return;

        await dbContext.Messages.AddAsync(new Message
        {
            Id = Guid.CreateVersion7(),
            ChatId = request.ChatId,
            Content = request.Content,
            Date = request.Timestamp.UtcDateTime,
        }, token);

        var usersForChat = await dbContext.ChatsUsers
            .Where(c => c.ChatId == request.ChatId)
            .Select(c => c.UserId)
            .ToArrayAsync(token);

        await dbContext.SaveChangesAsync(token);

        var connectionsIds = await storage.Get(usersForChat);

        var message = ctx.Serializer.Serialize(
            new ChatMessage
            {
                Key = "chat/message/receive",
                Content = request.Content,
                Timestamp = request.Timestamp,
                ChatId = request.ChatId,
            });
        await connectionManager.SendAsync(connectionsIds, message, token);
    }
}
