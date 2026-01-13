namespace WebSockets.Otp.Core.Extensions;

internal static class AsyncEnumerableExtensions
{
    internal static async ValueTask<T> FirstOrDefaultAsync<T>(this IAsyncEnumerable<T> values, CancellationToken token = default)
    {
        await using var enumerator = values.GetAsyncEnumerator(token);

        if (await enumerator.MoveNextAsync())
            return enumerator.Current;

        return default!;
    }
}
