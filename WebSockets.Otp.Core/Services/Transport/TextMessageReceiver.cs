using System.Buffers;
using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Contracts.Transport;

namespace WebSockets.Otp.Core.Services.Transport;

public sealed class TextMessageReceiver : IMessageReceiver
{
    public const byte RecordSeparator = 0x1e;

    public string Protocol => "json";

    public async ValueTask Receive(ConnectionContext context, IMessageBuffer messageBuffer, CancellationToken token)
    {
        var reader = context.Transport.Input;

        while (true)
        {
            var result = await reader.ReadAsync(token);

            var buffer = result.Buffer;
            var bufferEnd = buffer.End;

            var separator = buffer.PositionOf(RecordSeparator);

            if (separator is not null)
                buffer = buffer.Slice(0, separator.Value);

            messageBuffer.Write(buffer);

            reader.AdvanceTo(bufferEnd);

            if (separator is not null)
                return;

            if (result.IsCompleted)
                return;
        }
    }
}
