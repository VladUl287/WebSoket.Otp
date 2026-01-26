using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Enums;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core.Services.Processors;

public sealed class ParallelMessageProcessor(
    IMessageDispatcher dispatcher, IMessageEnumerator enumerator, IAsyncObjectPool<IMessageBuffer> bufferPool,
    ILogger<ParallelMessageProcessor> logger) : IMessageProcessor
{
    public ProcessingMode Mode => ProcessingMode.Parallel;

    public async Task Process(
        IGlobalContext globalContext, ISerializer serializer, WsConfiguration options,
        CancellationToken token)
    {
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
            TaskScheduler = options.TaskScheduler,
            CancellationToken = token
        };

        var messages = enumerator.EnumerateAsync(globalContext.Socket, options, bufferPool, token);

        await Parallel.ForEachAsync(messages, parallelOptions, async (buffer, token) =>
        {
            try
            {
                await dispatcher.DispatchMessage(globalContext, serializer, buffer, token);
            }
            catch (Exception ex)
            {
                logger.LogDispatchMessageFailure(ex);
            }
            finally
            {
                buffer.SetLength(0);

                if (options.ShrinkBuffers)
                    buffer.Shrink();

                await bufferPool.Return(buffer, token);
            }
        });
    }
}
