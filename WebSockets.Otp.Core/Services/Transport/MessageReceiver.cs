using Microsoft.AspNetCore.Connections;
using System.Buffers;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Contracts.Transport;

namespace WebSockets.Otp.Core.Services.Transport;

public sealed class MessageReceiver : IMessageReceiver
{
    public async ValueTask Receive(ConnectionContext context, IMessageBuffer messageBuffer, CancellationToken token)
    {
        var reader = context.Transport.Input;

        while (true)
        {
            var result = await reader.ReadAsync(token);

            var buffer = result.Buffer;

            if (TryReadWebSocketFrame(ref buffer, messageBuffer))
            {
                return;
            }
            else
            {
                reader.AdvanceTo(buffer.Start, buffer.End);
            }

            if (result.IsCompleted)
                return;
        }
    }


    private static bool TryReadWebSocketFrame(ref ReadOnlySequence<byte> buffer, IMessageBuffer messageBuffer)
    {
        if (buffer.Length < 2) // Minimum header size
            return false;

        var reader = new SequenceReader<byte>(buffer);

        // Read first 2 bytes
        if (!reader.TryRead(out byte firstByte) || !reader.TryRead(out byte secondByte))
            return false;

        bool final = (firstByte & 0x80) != 0;

        bool masked = (secondByte & 0x80) != 0;
        long payloadLength = secondByte & 0x7F;

        // Handle extended payload lengths
        if (payloadLength == 126)
        {
            if (reader.Remaining < 2)
                return false;

            if (!reader.TryReadBigEndian(out short extendedLength))
                return false;

            payloadLength = extendedLength;
        }
        else if (payloadLength == 127)
        {
            if (reader.Remaining < 8)
                return false;

            if (!reader.TryReadBigEndian(out long longExtendedLength))
                return false;

            payloadLength = longExtendedLength;
        }

        // Read masking key if present
        byte[] maskingKey = null;
        if (masked)
        {
            if (reader.Remaining < 4)
                return false;

            maskingKey = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                if (!reader.TryRead(out maskingKey[i]))
                    return false;
            }
        }

        // Check if we have enough data for the payload
        if (reader.Remaining < payloadLength)
            return false;

        // Read and write payload
        var payload = buffer.Slice(reader.Position, payloadLength);
        foreach (var part in payload)
            messageBuffer.Write(part.Span);

        // Advance the buffer past what we consumed
        buffer = buffer.Slice(reader.Position.GetInteger() + payloadLength);

        return final;
    }
}
