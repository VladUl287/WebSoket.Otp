using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.Text;
using System.Text.Json;
using WebSockets.Otp.Core.Helpers;

namespace WebSockets.Otp.Benchmark;

[MemoryDiagnoser]
public class ExtractFieldBenchmark
{
    public ReadOnlyMemory<byte> SmallMessageBytes;
    public ReadOnlyMemory<byte> WorstOrderMessageBytes;

    [GlobalSetup]
    public void Setup()
    {
        var usualMessage = "{\"key\":\"chat-message\",\"name\":\"randyLahey\",\"email\":\"email\",\"content\":\"lorem Ipsum is simply dummy text of the printing and typesetting industry.\",\"timestamp\":\"1760116804\",\"phone\":{\"country\":\"RU\",\"number\":\"+79815562830\"},\"active\":false,\"admin\":true,\"roles\":[\"client\",\"admin\",\"randy\",\"lahey\"]}";
        SmallMessageBytes = Encoding.UTF8.GetBytes(usualMessage);

        var worstOrderMessage = "{\"name\":\"randyLahey\",\"email\":\"email\",\"content\":\"lorem Ipsum is simply dummy text of the printing and typesetting industry.\",\"timestamp\":\"1760116804\",\"phone\":{\"country\":\"RU\",\"number\":\"+79815562830\"},\"active\":false,\"admin\":true,\"roles\":[\"client\",\"admin\",\"randy\",\"lahey\"],\"key\":\"chat-message\"}";
        WorstOrderMessageBytes = Encoding.UTF8.GetBytes(worstOrderMessage);
    }

    public const string KeyField = "key";
    public ReadOnlyMemory<char> KeyFieldMemory = "key".AsMemory();

    [Benchmark]
    public string? Small_ExtractField_Parse()
    {
        using var doc = JsonDocument.Parse(SmallMessageBytes);
        doc.RootElement.TryGetProperty(KeyField, out var element);
        return element.GetString();
    }

    [Benchmark]
    public string? Small_ExtractField_Reader_Optimized()
    {
        var reader = new Utf8JsonReader(SmallMessageBytes.Span);
        var keyField = KeyFieldMemory.Span;
        while (reader.Read())
        {
            if (reader.TokenType is not JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals(keyField))
            {
                reader.Read();
                return reader.GetString();
            }

            reader.Skip();
        }
        return null;
    }

    [Benchmark]
    public string? Small_ExtractField_Reader_Interned_Custom()
    {
        var reader = new Utf8JsonReader(SmallMessageBytes.Span);
        var keyField = KeyFieldMemory.Span;
        while (reader.Read())
        {
            if (reader.TokenType is not JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals(keyField))
            {
                reader.Read();
                return StringIntern.Intern(reader.ValueSpan);
            }

            reader.Skip();
        }
        return null;
    }

    [Benchmark]
    public string? Small_ExtractField_Reader()
    {
        var reader = new Utf8JsonReader(SmallMessageBytes.Span);

        string? property = null;
        while (reader.Read())
        {
            var tokenType = reader.TokenType;
            switch (tokenType)
            {
                case JsonTokenType.PropertyName:
                    property = reader.GetString();
                    break;
                case JsonTokenType.String when property == KeyField:
                    return reader.GetString();
            }
        }

        return null;
    }

    [Benchmark]
    public string? Worst_ExtractField_Parse()
    {
        using var doc = JsonDocument.Parse(WorstOrderMessageBytes);
        doc.RootElement.TryGetProperty(KeyField, out var element);
        return element.GetString();
    }

    [Benchmark]
    public string? Worst_ExtractField_Reader()
    {
        var reader = new Utf8JsonReader(WorstOrderMessageBytes.Span);

        string? property = null;
        while (reader.Read())
        {
            var tokenType = reader.TokenType;
            switch (tokenType)
            {
                case JsonTokenType.PropertyName:
                    property = reader.GetString();
                    break;
                case JsonTokenType.String when property == KeyField:
                    return reader.GetString();
            }
        }

        return null;
    }

    [Benchmark]
    public string? Worst_ExtractField_Reader_Optimized()
    {
        var reader = new Utf8JsonReader(WorstOrderMessageBytes.Span);

        ReadOnlySpan<char> keyField = KeyFieldMemory.Span;

        while (reader.Read())
        {
            if (reader.TokenType is not JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals(keyField))
            {
                reader.Read();
                return reader.GetString();
            }

            reader.Skip();
        }

        return null;
    }

    [Benchmark]
    public ReadOnlySpan<byte> Worst_ExtractField_Reader_Optimized_AsBytes()
    {
        var reader = new Utf8JsonReader(WorstOrderMessageBytes.Span);

        ReadOnlySpan<char> keyField = KeyFieldMemory.Span;

        while (reader.Read())
        {
            if (reader.TokenType is not JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals(keyField))
            {
                reader.Read();

                return reader.HasValueSequence ?
                    reader.ValueSequence.ToArray() :
                    reader.ValueSpan;
            }

            reader.Skip();
        }

        return null;
    }
}
