namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsMessageProcessingOptions
{
    public ProcessingMode Mode { get; set; } = ProcessingMode.Sequential;
    public int MaxParallelOperations { get; set; } = 10;
}

public sealed class ProcessingMode : IEquatable<ProcessingMode>
{
    public string Value { get; }

    private ProcessingMode(string value) => Value = value;

    public static readonly ProcessingMode Sequential = new("sequential");
    public static readonly ProcessingMode Parallel = new("parallel");
    //public static readonly ProcessingMode Priorities = new("priorities");
    //public static readonly ProcessingMode Batch = new("batch");
    //public static readonly ProcessingMode Throttled = new("throttled");

    public static ProcessingMode New(string mode) => new(mode);

    public override string ToString() => Value;
    public bool Equals(ProcessingMode? other) => other?.Value == Value;
    public override bool Equals(object? obj) => obj is ProcessingMode other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(ProcessingMode? left, ProcessingMode? right) =>
        Equals(left, right);
    public static bool operator !=(ProcessingMode? left, ProcessingMode? right) =>
        !Equals(left, right);

    public static implicit operator string(ProcessingMode mode) => mode.Value;
}