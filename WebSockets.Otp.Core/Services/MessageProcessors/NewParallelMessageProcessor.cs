using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Contracts.Transport;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Services.MessageProcessors;

public class NewParallelMessageProcessor(
    IMessageEnumerator enumerator, IMessageDispatcher dispatcher, ISerializerResolver serializerFactory) : INewMessageProcessor
{
    public string Name => ProcessingMode.Parallel;

    public async Task Process(ConnectionContext context, IWsConnection connection,
        WsMiddlewareOptions options, WsConnectionOptions connectionOptions, CancellationToken token)
    {
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = options.Processing.MaxParallelOperations,
            CancellationToken = token
        };

        if (!serializerFactory.TryResolve(connectionOptions.Protocol, out var serializer))
            return;

        var messages = enumerator.EnumerateAsync(context, options, token);

        await Parallel.ForEachAsync(messages, parallelOptions, async (buffer, token) =>
        {
            try
            {
                await dispatcher.DispatchMessage(connection, serializer, buffer, token);
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
