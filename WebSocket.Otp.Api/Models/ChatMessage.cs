using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Api.Models;

public sealed class ChatMessage : WsMessage
{
    public string Content { get; init; } = string.Empty;
}