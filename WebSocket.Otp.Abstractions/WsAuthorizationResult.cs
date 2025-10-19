namespace WebSockets.Otp.Abstractions;

public sealed record WsAuthorizationResult(bool IsAuthorized, int? StatusCode = null, string? FailureReason = null);
