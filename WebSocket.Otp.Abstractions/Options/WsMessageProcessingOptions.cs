namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsMessageProcessingOptions
{
    public string Mode { get; set; } = ProcessingMode.Parallel;
    public int MaxParallel { get; set; } = Environment.ProcessorCount * 10;
}
