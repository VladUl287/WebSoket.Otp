using BenchmarkDotNet.Attributes;
using System.Text;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Services.Serializers;
using WebSockets.Otp.Core.Utils;

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
    public ReadOnlyMemory<char> KeyFieldMemoryChar = "key".AsMemory();
    public ReadOnlyMemory<byte> KeyFieldMemory = Encoding.UTF8.GetBytes("key").AsMemory();


    private static readonly JsonMessageSerializer jsonMessageSerializer = new();
    private static readonly IStringPool stringPool = new PreloadedStringPool(["chat-message"], Encoding.UTF8);
    private static readonly IStringPool unsafeStringPool = new PreloadedStringPool(["chat-message"], Encoding.UTF8, true);

    [Benchmark]
    public string? Small_ExtractField_Parse()
    {
        using var doc = JsonDocument.Parse(SmallMessageBytes);
        doc.RootElement.TryGetProperty(KeyField, out var element);
        return element.GetString();
    }

    [Benchmark]
    public string? Small_ExtractStringField_Field_String()
    {
        return jsonMessageSerializer.ExtractStringField(KeyField, SmallMessageBytes.Span);
    }

    [Benchmark]
    public string? Small_ExtractStringField_Field_Span()
    {
        return jsonMessageSerializer.ExtractStringField(KeyFieldMemory.Span, SmallMessageBytes.Span);
    }

    [Benchmark]
    public string? Small_ExtractStringField_Field_Span_Pool()
    {
        return jsonMessageSerializer.ExtractStringField(KeyFieldMemory.Span, SmallMessageBytes.Span, stringPool);
    }

    [Benchmark]
    public string? Small_ExtractStringField_Field_Span_Unsafe_Pool()
    {
        return jsonMessageSerializer.ExtractStringField(KeyFieldMemory.Span, SmallMessageBytes.Span, unsafeStringPool);
    }

    private static readonly CommunityToolkit.HighPerformance.Buffers.StringPool toolkitStringPool = new();

    [Benchmark]
    public string? Small_ExtractStringField_Field_Pool_Toolkit()
    {
        var reader = new Utf8JsonReader(SmallMessageBytes.Span);
        var keyField = KeyField.AsSpan();
        while (reader.Read())
        {
            if (reader.TokenType is not JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals(keyField))
            {
                reader.Read();
                return toolkitStringPool.GetOrAdd(reader.ValueSpan, Encoding.UTF8);
            }

            reader.Skip();
        }
        return null;
    }

    //[Benchmark]
    //public string? Worst_ExtractField_Parse()
    //{
    //    using var doc = JsonDocument.Parse(WorstOrderMessageBytes);
    //    doc.RootElement.TryGetProperty(KeyField, out var element);
    //    return element.GetString();
    //}

    //[Benchmark]
    //public string? Worst_ExtractField_Reader()
    //{
    //    var reader = new Utf8JsonReader(WorstOrderMessageBytes.Span);

    //    string? property = null;
    //    while (reader.Read())
    //    {
    //        var tokenType = reader.TokenType;
    //        switch (tokenType)
    //        {
    //            case JsonTokenType.PropertyName:
    //                property = reader.GetString();
    //                break;
    //            case JsonTokenType.String when property == KeyField:
    //                return reader.GetString();
    //        }
    //    }

    //    return null;
    //}

    //[Benchmark]
    //public string? Worst_ExtractField_Reader_Optimized()
    //{
    //    var reader = new Utf8JsonReader(WorstOrderMessageBytes.Span);

    //    ReadOnlySpan<char> keyField = KeyFieldMemory.Span;

    //    while (reader.Read())
    //    {
    //        if (reader.TokenType is not JsonTokenType.PropertyName)
    //            continue;

    //        if (reader.ValueTextEquals(keyField))
    //        {
    //            reader.Read();
    //            return reader.GetString();
    //        }

    //        reader.Skip();
    //    }

    //    return null;
    //}

    //[Benchmark]
    //public ReadOnlySpan<byte> Worst_ExtractField_Reader_Optimized_AsBytes()
    //{
    //    var reader = new Utf8JsonReader(WorstOrderMessageBytes.Span);

    //    ReadOnlySpan<char> keyField = KeyFieldMemory.Span;

    //    while (reader.Read())
    //    {
    //        if (reader.TokenType is not JsonTokenType.PropertyName)
    //            continue;

    //        if (reader.ValueTextEquals(keyField))
    //        {
    //            reader.Read();

    //            return reader.HasValueSequence ?
    //                reader.ValueSequence.ToArray() :
    //                reader.ValueSpan;
    //        }

    //        reader.Skip();
    //    }

    //    return null;
    //}
}
