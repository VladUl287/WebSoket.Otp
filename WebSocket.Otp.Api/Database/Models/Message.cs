using Microsoft.EntityFrameworkCore;

namespace WebSockets.Otp.Api.Database.Models;

[PrimaryKey(nameof(Id), nameof(ChatId))]
public sealed class Message
{
    public Guid Id { get; init; }
    public Guid ChatId { get; init; }
    public required string Content { get; init; }
    public DateTime Date { get; init; }

    public Chat? Chat { get; init; }
}
