namespace WebSockets.Otp.Abstractions.Results;

public readonly struct AuthValidationResult
{
    public int StatusCode { get; }
    public string FailureReason { get; }

    public bool Succeeded { get; }
    public bool Failed => !Succeeded && StatusCode != default;

    private AuthValidationResult(bool succeeded, int statusCode, string failureReason)
    {
        Succeeded = succeeded;
        StatusCode = statusCode;
        FailureReason = failureReason;
    }

    public static AuthValidationResult Success() => new(true, default, string.Empty);
    public static AuthValidationResult Failure(int statusCode, string failureReason)
    {
        if (statusCode < 100 || statusCode > 599)
            throw new ArgumentOutOfRangeException(nameof(statusCode), "Status code must be between 100 and 599");

        return new(false, statusCode, failureReason);
    }
}