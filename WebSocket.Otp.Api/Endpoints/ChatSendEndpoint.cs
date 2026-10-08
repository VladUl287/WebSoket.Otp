using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Api.Database;
using WebSockets.Otp.Api.Database.Models;
using WebSockets.Otp.Api.Models;

namespace WebSockets.Otp.Api.Endpoints;

//[Authorize(Policy = "test")]
[WsEndpoint("chat/message/send")]
public sealed class ChatSendEndpoint :
    WsEndpoint<ChatMessage, ChatMessage>
{
    public override async Task<ChatMessage> HandleAsync(ChatMessage request, EndpointContext ctx)
    {
        var result = new ChatMessage
        {
            Content = request.Content,
            Timestamp = request.Timestamp,
            ChatId = request.ChatId,
        };

        await ctx.Send
           .All()
           .SendAsync("notify/message/receive", new
           {
               Message = request.Content
           }, default);

        return result;
    }
}
