using Microsoft.AspNetCore.Connections;
using System.Buffers;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Services.Transport;

public sealed class JsonMessageReader : IMessageReader
{
    public string ProtocolName => "json";

    public async ValueTask Receive(ConnectionContext context, IMessageBuffer messageBuffer, CancellationToken token)
    {
        var reader = context.Transport.Input;

        while (true)
        {
            var result = await reader.ReadAsync(token);

            var buffer = result.Buffer;
            var bufferEnd = buffer.End;

            var separator = buffer.PositionOf(MessageConstants.JsonRecordSeparator);

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
