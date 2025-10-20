using System.Diagnostics.CodeAnalysis;

namespace WebSockets.Otp.Abstractions.Results;

public readonly struct WsAuthorizationResult
{
    [MemberNotNullWhen(false, nameof(FailureReason))]
    public bool Succeeded { get; }

    [MemberNotNullWhen(true, nameof(FailureReason))]
    public bool Failed => !Succeeded && FailureReason is not null;

    public string? FailureReason { get; }

    private WsAuthorizationResult(bool succeeded, string? failureReason)
    {
        Succeeded = succeeded;
        FailureReason = failureReason;
    }

    public static WsAuthorizationResult Success() => new(true, null);
    public static WsAuthorizationResult Failure(string failureReason)
    {
        ArgumentNullException.ThrowIfNull(failureReason, nameof(failureReason));
        return new(false, failureReason);
    }
}
