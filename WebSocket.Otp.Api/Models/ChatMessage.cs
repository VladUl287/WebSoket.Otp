using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Api.Models;

public sealed record ChatMessage(string Route, string UserName, string Content, DateTime Timestamp) : IWsMessage;