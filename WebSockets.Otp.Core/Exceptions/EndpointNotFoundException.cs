namespace WebSockets.Otp.Core.Exceptions;

public sealed class EndpointNotFoundException(string message) : InvalidOperationException(message)
{ }
