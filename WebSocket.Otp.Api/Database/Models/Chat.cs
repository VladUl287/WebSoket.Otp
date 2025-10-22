namespace WebSockets.Otp.Api.Database.Models;

public sealed class Chat
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public IEnumerable<Message> Messages { get; init; } = [];
}
