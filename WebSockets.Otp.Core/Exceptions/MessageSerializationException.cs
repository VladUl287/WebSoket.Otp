namespace WebSockets.Otp.Core.Exceptions;

public sealed class MessageSerializationException(string message, Exception? inner = null) : InvalidOperationException(message, inner)
{ }
