using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Services.Processors;

public sealed class ParallelMessageProcessor(
    IMessageDispatcher dispatcher, IAsyncObjectPool<IMessageBuffer> bufferPool) : IMessageProcessor
{
    public string ProcessingMode => Abstractions.Options.ProcessingMode.Parallel;

    public async Task Process(
        IMessageEnumerator enumerator, IGlobalContext globalContext, ISerializer serializer, 
        WsBaseOptions options, CancellationToken token)
    {
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = options.ProcessingMaxDegreeOfParallelilism,
            CancellationToken = token
        };

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
