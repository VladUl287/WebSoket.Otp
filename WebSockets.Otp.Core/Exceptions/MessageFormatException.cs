namespace WebSockets.Otp.Core.Exceptions;

public sealed class MessageFormatException(string message) : FormatException(message)
{ }
