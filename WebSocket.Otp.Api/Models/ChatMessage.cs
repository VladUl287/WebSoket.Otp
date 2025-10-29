using WebSockets.Otp.Abstractions;

namespace WebSockets.Otp.Api.Models;

public sealed class ChatMessage : WsMessage
{
    public Guid ChatId { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
}