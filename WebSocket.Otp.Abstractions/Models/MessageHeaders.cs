namespace WebSockets.Otp.Abstractions.Models;

public enum MessageType
{
    Response,
    Error
}

public sealed class MessageHeaders
{
    public string? Key { get; set; }
    public uint? CorrelationId { get; set; }
    public MessageType Type { get; set; }
}
