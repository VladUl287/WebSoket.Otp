namespace WebSockets.Otp.Abstractions.Utils;

public readonly struct Result<TSuccess, TError>(int index, TSuccess success = default, TError error = default)
{
    private readonly TSuccess _success = success;
    private readonly TError _error = error;
    private readonly int _index = index;

    public int Index => _index;
    public bool IsSuccess => _index == 0;
    public bool IsError => _index == 1;


    public static implicit operator Result<TSuccess, TError>(TSuccess t) => new(0, t, default);
    public static implicit operator Result<TSuccess, TError>(TError t) => new(1, default, t);

    public TSuccess Value =>
        _index == 0 ? _success : throw new InvalidOperationException($"Cannot return as First as result is Second");

    public TError Error =>
        _index == 1 ? _error : throw new InvalidOperationException($"Cannot return as Second as result is First");
}