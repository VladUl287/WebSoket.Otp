using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Extensions;

public static class WsConnectionExtensions
{
    public static IWsContext AsPublicContext(this IWsConnection conn) => new SimpleWsContext(conn);

    private class SimpleWsContext(IWsConnection conn) : IWsContext
    {
        public IWsConnection Connection => conn;
        public CancellationToken Cancellation => CancellationToken.None;

        public ValueTask SendAsync<T>(T message, CancellationToken token) where T : IWsMessage
        {
            // This simple mapping expects a registered IMessageSerializer in the DI; to get it we would need IServiceProvider.
            // In Program.cs we used dispatcher.DispatchMessage(conn.AsPublicContext(), payload, token) where publicCtx is lightweight.
            throw new NotImplementedException("Prefer using dispatcher's public context created inside dispatcher with DI scope.");
        }
    }
}
