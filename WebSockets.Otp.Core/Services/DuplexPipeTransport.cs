using System.IO.Pipelines;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services;

public sealed class DuplexPipeTransport(IDuplexPipe duplexPipe, ISerializer serializer) : IConnectionTransport
{
    public async ValueTask SendAsync<TData>(TData data, CancellationToken token)
        where TData : notnull
    {
        var message = serializer.Serialize(data);
        await duplexPipe.Output.WriteAsync(message, token);
    }
}
