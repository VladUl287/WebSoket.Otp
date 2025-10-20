using Microsoft.EntityFrameworkCore;

namespace WebSockets.Otp.Api.Database.Models;

[PrimaryKey(nameof(UserId), nameof(ChatId))]
public sealed class ChatUser
{
    public long UserId { get; init; }
    public User? User { get; init; }

    public Guid ChatId { get; init; }
    public Chat? Chat { get; init; }
}
