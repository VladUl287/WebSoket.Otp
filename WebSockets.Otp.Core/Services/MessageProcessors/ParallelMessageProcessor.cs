using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Services.MessageProcessors;

public sealed class ParallelMessageProcessor(
    IMessageEnumerator enumerator, IMessageDispatcher dispatcher, ISerializerResolver serializerFactory,
    IMessageReceiverResolver messageReceiverResolver, IAsyncObjectPool<IMessageBuffer> bufferPool) : IMessageProcessor
{
    public string ProcessingMode => Abstractions.Options.ProcessingMode.Parallel;

    public async Task Process(
        ConnectionContext context, IGlobalContext globalContext, WsMiddlewareOptions options,
        WsConnectionOptions connectionOptions, CancellationToken token)
    {
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = options.Processing.MaxParallel,
            CancellationToken = token
        };

        if (!serializerFactory.TryResolve(connectionOptions.Protocol, out var serializer))
            return;

        if (!messageReceiverResolver.TryResolve(connectionOptions.Protocol, out var messageReceiver))
            return;

        var messages = enumerator.EnumerateAsync(messageReceiver, context, options, bufferPool, token);
        await Parallel.ForEachAsync(messages, parallelOptions, async (buffer, token) =>
        {
            try
            {
                await dispatcher.DispatchMessage(globalContext, serializer, buffer, token);
            }
            finally
            {
                buffer.SetLength(0);

                if (options.Memory.ReclaimBuffersImmediately)
                    buffer.Shrink();

                await bufferPool.Return(buffer, token);
            }
        });
    }
}
