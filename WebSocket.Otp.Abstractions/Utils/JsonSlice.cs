namespace WebSockets.Otp.Abstractions.Utils;

public readonly struct JsonSlice(int start, int end)
{
    public readonly int Start = start;
    public readonly int End = end;
    public readonly bool Found = true;
}
