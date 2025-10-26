//using BenchmarkDotNet.Attributes;
//using Microsoft.AspNetCore.Http;
//using Moq;
//using System.Net.WebSockets;
//using WebSockets.Otp.Abstractions.Contracts;
//using WebSockets.Otp.Core.Middlewares;

//namespace WebSockets.Otp.Benchmark;

//[MemoryDiagnoser]
//[ThreadingDiagnoser]
//public class WebSocketLoopBenchmark
//{
//    private Mock<HttpContext> _contextMock;
//    private Mock<IWsConnection> _wsConnectionMock;
//    private Mock<WebSocket> _webSocketMock;
//    private Mock<IMessageDispatcher> _dispatcherMock;
//    private Mock<IServiceProvider> _serviceProviderMock;
//    private CancellationTokenSource _cancellationTokenSource;

//    [GlobalSetup]
//    public void Setup()
//    {
//        _cancellationTokenSource = new CancellationTokenSource();

//        _contextMock = new Mock<HttpContext>();
//        _wsConnectionMock = new Mock<IWsConnection>();
//        _webSocketMock = new Mock<WebSocket>();
//        _dispatcherMock = new Mock<IMessageDispatcher>();
//        _serviceProviderMock = new Mock<IServiceProvider>();

//        // Setup WebSocket state
//        _webSocketMock.Setup(x => x.State).Returns(WebSocketState.Open);

//        // Setup connection
//        _wsConnectionMock.Setup(x => x.Socket).Returns(_webSocketMock.Object);
//        _wsConnectionMock.Setup(x => x.CloseAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
//                        .Returns(Task.CompletedTask);

//        // Setup service provider
//        _serviceProviderMock.Setup(x => x.GetService(typeof(IMessageDispatcher)))
//                          .Returns(_dispatcherMock.Object);
//        _contextMock.Setup(x => x.RequestServices).Returns(_serviceProviderMock.Object);
//        _contextMock.Setup(x => x.RequestAborted).Returns(_cancellationTokenSource.Token);

//        // Setup dispatcher
//        _dispatcherMock.Setup(x => x.DispatchMessage(It.IsAny<IWsConnection>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
//            .Returns(Task.CompletedTask);
//    }

//    [GlobalCleanup]
//    public void Cleanup()
//    {
//        _cancellationTokenSource?.Dispose();
//    }

//    [Benchmark]
//    public async Task SocketLoop_SmallMessages()
//    {
//        // Simulate receiving 100 small messages
//        int messageCount = 0;
//        var expectedMessages = 100;

//        _webSocketMock.Setup(x => x.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
//                     .ReturnsAsync((ArraySegment<byte> buffer, CancellationToken token) =>
//                     {
//                         if (messageCount >= expectedMessages)
//                         {
//                             return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
//                         }

//                         messageCount++;

//                         // Simulate small message (100 bytes)
//                         var smallMessage = new byte[100];
//                         new Random().NextBytes(smallMessage);

//                         // Copy to buffer
//                         Array.Copy(smallMessage, 0, buffer.Array, buffer.Offset, Math.Min(smallMessage.Length, buffer.Count));

//                         return new WebSocketReceiveResult(
//                             Math.Min(smallMessage.Length, buffer.Count),
//                             WebSocketMessageType.Binary,
//                             messageCount == expectedMessages); // Last message is EndOfMessage
//                     });

//        await WsMiddleware.SocketLoop(
//            _contextMock.Object,
//            _wsConnectionMock.Object,
//            new WsMiddlewareOptions()
//        );
//    }

//    [Benchmark]
//    public async Task SocketLoop_LargeMessages()
//    {
//        int messageCount = 0;
//        var expectedMessages = 10;

//        _webSocketMock.Setup(x => x.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
//                     .ReturnsAsync((ArraySegment<byte> buffer, CancellationToken token) =>
//                     {
//                         if (messageCount >= expectedMessages)
//                         {
//                             return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
//                         }

//                         messageCount++;

//                         // Simulate large message (8KB)
//                         var largeMessage = new byte[8 * 1024];
//                         new Random().NextBytes(largeMessage);

//                         // Copy to buffer
//                         Array.Copy(largeMessage, 0, buffer.Array, buffer.Offset, Math.Min(largeMessage.Length, buffer.Count));

//                         return new WebSocketReceiveResult(
//                             Math.Min(largeMessage.Length, buffer.Count),
//                             WebSocketMessageType.Binary,
//                             messageCount == expectedMessages);
//                     });

//        await WsMiddleware.SocketLoop(
//            _contextMock.Object,
//            _wsConnectionMock.Object,
//            new WsMiddlewareOptions()
//        );
//    }

//    [Benchmark]
//    public async Task SocketLoop_FragmentedMessages()
//    {
//        // Simulate receiving 5 messages fragmented into 3 chunks each
//        int messageCount = 0;
//        int chunkCount = 0;
//        var expectedMessages = 5;
//        var chunksPerMessage = 3;

//        _webSocketMock.Setup(x => x.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
//                     .ReturnsAsync((ArraySegment<byte> buffer, CancellationToken token) =>
//                     {
//                         if (messageCount >= expectedMessages)
//                         {
//                             return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
//                         }

//                         chunkCount++;
//                         bool isEndOfMessage = chunkCount % chunksPerMessage == 0;

//                         if (isEndOfMessage)
//                         {
//                             messageCount++;
//                         }

//                         // Simulate chunk (2KB)
//                         var chunk = new byte[2 * 1024];
//                         new Random().NextBytes(chunk);

//                         Array.Copy(chunk, 0, buffer.Array, buffer.Offset, Math.Min(chunk.Length, buffer.Count));

//                         return new WebSocketReceiveResult(
//                             Math.Min(chunk.Length, buffer.Count),
//                             WebSocketMessageType.Binary,
//                             isEndOfMessage);
//                     });

//        await WsMiddleware.SocketLoop(
//            _contextMock.Object,
//            _wsConnectionMock.Object,
//            new WsMiddlewareOptions()
//        );
//    }
//}