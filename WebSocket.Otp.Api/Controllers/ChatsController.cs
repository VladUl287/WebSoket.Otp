using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebSockets.Otp.Api.Database;

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
}
