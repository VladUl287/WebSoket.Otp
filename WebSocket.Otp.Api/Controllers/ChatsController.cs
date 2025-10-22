using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebSockets.Otp.Api.Database;
using WebSockets.Otp.Api.Models;

namespace WebSockets.Otp.Api.Controllers;

[Authorize]
[Route("[controller]/[action]")]
public class ChatsController(DatabaseContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await dbContext.Chats.ToArrayAsync();
        return Ok(result);
    }

    [HttpGet("{chatId:guid}")]
    public async Task<IActionResult> GetMessages([FromRoute] Guid chatId)
    {
        var userId = User.GetUserId<long>();
        var exists = await dbContext.ChatsUsers.AnyAsync(c => c.UserId == userId && c.ChatId == chatId);
        if (!exists)
            return NotFound();

        var result = await dbContext.Chats
            .Where(c => c.Id == chatId)
            .SelectMany(c => c.Messages.Select(cm => new ChatMessage
            {
                ChatId = chatId,
                Content = cm.Content,
                Timestamp = cm.Date
            }))
            .ToArrayAsync();
        return Ok(result);
    }
}
