namespace WebSockets.Otp.Core.Extensions;

internal static class AsyncEnumerableExtensions
{
    internal static async ValueTask<T> FirstOrDefault<T>(this IAsyncEnumerable<T> values)
    {
        await using var enumerator = values.GetAsyncEnumerator();

        if (await enumerator.MoveNextAsync())
            return enumerator.Current;

        return default!;
    }
}
