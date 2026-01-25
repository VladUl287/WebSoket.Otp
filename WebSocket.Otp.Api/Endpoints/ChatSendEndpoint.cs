using Microsoft.EntityFrameworkCore;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Api.Database;
using WebSockets.Otp.Api.Database.Models;
using WebSockets.Otp.Api.Models;

namespace WebSockets.Otp.Api.Endpoints;

[WsEndpoint("chat/message/send")]
public sealed class ChatSendEndpoint(DatabaseContext dbContext) :
    WsEndpoint<ChatMessage>
{
    public override async Task HandleAsync(ChatMessage request, EndpointContext ctx)
    {
        var token = ctx.Cancellation;
        var userId = ctx.Context.User.GetUserId<long>();

        var userInChat = await dbContext.ChatsUsers
            .AnyAsync(c => c.UserId == userId && c.ChatId == request.ChatId, token);

        if (!userInChat)
            return;

        await dbContext.Messages.AddAsync(new Message
        {
            Id = Guid.CreateVersion7(),
            ChatId = request.ChatId,
            Content = request.Content,
            Date = request.Timestamp.UtcDateTime,
        }, token);

        await dbContext.SaveChangesAsync(token);

        var usersIds = dbContext.ChatsUsers
            .Where(c => c.ChatId == request.ChatId)
            .Select(c => c.UserId)
            .AsAsyncEnumerable();

        var message = new ChatMessage
        {
            Content = request.Content,
            Timestamp = request.Timestamp,
            ChatId = request.ChatId,
        };

        const int SendThreshold = 100;
        var counter = 0;
        var send = ctx.Send;

        await foreach (var chatUser in usersIds)
        {
            if (token.IsCancellationRequested)
                break;

            send.Group(chatUser.ToString());

            if (counter > SendThreshold)
            {
                await send.SendAsync(message, token);
                send = ctx.Send;
                counter = 0;
            }
        }
    }
}
