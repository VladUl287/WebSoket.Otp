namespace WebSockets.Otp.Abstractions.Utils;


public sealed class ProcessProtocol : IEquatable<ProcessProtocol>
{
    public string Value { get; init; }

    public static readonly ProcessProtocol Json = new("json");
    public static readonly ProcessProtocol Protobuf = new("protobuf");
    public static readonly ProcessProtocol MessagePack = new("messagepack");

    public ProcessProtocol(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        Value = value;
    }

    public bool Equals(ProcessProtocol? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as ProcessProtocol);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public override string ToString() => Value;

    public static bool operator ==(ProcessProtocol? left, ProcessProtocol? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(ProcessProtocol? left, ProcessProtocol? right) =>
        !(left == right);

    public static implicit operator string(ProcessProtocol protocol) => protocol.Value;

    public static explicit operator ProcessProtocol(string value) => new(value);

    public static ProcessProtocol Create(string value) => new(value);
}