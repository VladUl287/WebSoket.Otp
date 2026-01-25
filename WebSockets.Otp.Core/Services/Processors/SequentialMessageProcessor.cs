using System.Buffers;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Enums;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services.Processors;

public sealed class SequentialMessageProcessor(IMessageDispatcher dispatcher, IMessageBufferFactory bufferFactory) : IMessageProcessor
{
    private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Create();

    public ProcessingMode Mode => ProcessingMode.Sequential;

    public async Task Process(IGlobalContext globalContext, ISerializer serializer, WsConfiguration config, CancellationToken token)
    {
        using var buffer = bufferFactory.Create(config.ReceiveBufferSize);
        var receiveBuffer = _arrayPool.Rent(config.ReceiveBufferSize);
        var socket = globalContext.Socket;

        while (!token.IsCancellationRequested)
        {
            var receiveResult = await socket.ReceiveAsync(receiveBuffer, token);

            if (receiveResult is { MessageType: WebSocketMessageType.Close })
                break;

            if (receiveResult.Count > config.MaxMessageSize - buffer.Length)
                throw new OutOfMemoryException($"Message exceed maximum message size '{config.MaxMessageSize}'.");

            buffer.Write(receiveBuffer.AsSpan(0, receiveResult.Count));

            if (receiveResult.EndOfMessage)
            {
                try
                {
                    await dispatcher.DispatchMessage(globalContext, serializer, buffer, token);
                }
                finally
                {
                    buffer.SetLength(0);

                    if (config.ShrinkBuffers)
                        buffer.Shrink();
                }
            }
        }

        _arrayPool.Return(receiveBuffer);
    }
}
