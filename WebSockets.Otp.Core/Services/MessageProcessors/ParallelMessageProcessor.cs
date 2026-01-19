using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Services.MessageProcessors;

public sealed class ParallelMessageProcessor(
    IAsyncObjectPoolFactory poolFactory, IMessageBufferFactory bufferFactory, IMessageDispatcher dispatcher) : IMessageProcessor
{
    public string ProcessingMode => Abstractions.Options.ProcessingMode.Parallel;

    public async Task Process(
        IMessageEnumerator enumerator, IGlobalContext globalContext, ISerializer serializer, 
        WsMiddlewareOptions options, CancellationToken token)
    {
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = options.ProcessingMaxDegreeOfParallelilism,
            CancellationToken = token
        };

        await using var bufferPool = poolFactory.Create(options.ProcessingMaxDegreeOfParallelilism, () =>
        {
            return bufferFactory.Create(options.InitialMessageBufferSize);
        });

        var messages = enumerator.EnumerateAsync(bufferPool, token);

        await Parallel.ForEachAsync(messages, parallelOptions, async (buffer, token) =>
        {
            try
            {
                await dispatcher.DispatchMessage(globalContext, serializer, buffer, token);
            }
            finally
            {
                buffer.SetLength(0);

                if (options.ShrinkMessageBuffer)
                    buffer.Shrink();

                await bufferPool.Return(buffer, token);
            }
        });
    }
}
