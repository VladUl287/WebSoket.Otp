namespace WebSockets.Otp.Api.Database.Models;

public sealed class Chat
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Name { get; set; }
}
