using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services.MessageProcessors;

public class ParallelMessageProcessor(
    IMessageEnumerator enumerator, IMessageDispatcher dispatcher, ISerializerResolver serializerFactory,
    IMessageReceiverResolver messageReceiverResolver) : IMessageProcessor
{
    public string Mode => ProcessingMode.Parallel;

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

        var messages = enumerator.EnumerateAsync(messageReceiver, context, options, token);

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

                //await bufferPool.Return(buffer);
            }
        });
    }
}
