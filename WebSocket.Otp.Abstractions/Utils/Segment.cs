namespace WebSockets.Otp.Abstractions.Utils;

public readonly struct Segment(int start, int end)
{
    public readonly int Start = start;
    public readonly int End = end;
    public readonly bool Found = true;
}
