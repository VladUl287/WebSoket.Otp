using System.Net.WebSockets;

namespace WebSockets.Otp.Benchmark.Utils;

public class MockWebSocket(byte[] message, int count) : WebSocket
{
    private WebSocketState _state = WebSocketState.Open;

    public override WebSocketCloseStatus? CloseStatus => WebSocketCloseStatus.NormalClosure;
    public override string CloseStatusDescription => "Normal closure";
    public override WebSocketState State => _state;
    public override string SubProtocol => string.Empty;

    private static int Readed = 0;

    private readonly WebSocketReceiveResult defautlResult = new WebSocketReceiveResult(
        message.Length,
        WebSocketMessageType.Text,
        true);

    public override async Task<WebSocketReceiveResult> ReceiveAsync(
        ArraySegment<byte> buffer,
        CancellationToken cancellationToken)
    {
        if (Readed > count)
        {
            Readed = 0;
            return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
        }

        var bytesToCopy = Math.Min(message.Length, buffer.Count);
        Array.Copy(message, 0, buffer.Array, buffer.Offset, bytesToCopy);

        Readed++;
        return defautlResult;
    }

    public override Task SendAsync(
        ArraySegment<byte> buffer,
        WebSocketMessageType messageType,
        bool endOfMessage,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override Task CloseAsync(
        WebSocketCloseStatus closeStatus,
        string statusDescription,
        CancellationToken cancellationToken)
    {
        _state = WebSocketState.Closed;
        return Task.CompletedTask;
    }

    public override Task CloseOutputAsync(
        WebSocketCloseStatus closeStatus,
        string statusDescription,
        CancellationToken cancellationToken)
    {
        _state = WebSocketState.Closed;
        return Task.CompletedTask;
    }

    public override void Abort()
    {
        _state = WebSocketState.Closed;
    }

    public override void Dispose()
    {
        _state = WebSocketState.Closed;
    }
}