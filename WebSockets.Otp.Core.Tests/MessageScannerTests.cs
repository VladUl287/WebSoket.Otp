using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Services.Serializers;

namespace WebSockets.Otp.Core.Tests;

public class MessageScannerTests
{
    private readonly JsonSerializerOptions DefualtOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IgnoreReadOnlyProperties = false,
        IgnoreReadOnlyFields = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = false,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement
    };

    private IMessageSerializer CreateScanner => new JsonMessageSerializer(DefualtOptions);

    private static JsonSlice[] CreateResults() => new JsonSlice[3];

    private static byte[] ToUtf8(string s) => Encoding.UTF8.GetBytes(s);

    private static string SliceToString(ReadOnlySpan<byte> json, JsonSlice slice)
    {
        var sliceSpan = json[slice.Start..slice.End];
        return Encoding.UTF8.GetString(sliceSpan);
    }

    [Fact]
    public void ScanMessage_AllThreeKeysPresent_FindsAllSlices()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":\"value1\",\"correlationId\":\"corr1\",\"value\":\"val1\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.True(results[1].Found);
        Assert.True(results[2].Found);
        Assert.Equal("\"value1\"", SliceToString(json, results[0]));
        Assert.Equal("\"corr1\"", SliceToString(json, results[1]));
        Assert.Equal("\"val1\"", SliceToString(json, results[2]));
    }

    [Fact]
    public void ScanMessage_OnlyKeyPresent_FindsOnlyKeySlice()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":\"abc\",\"other\":\"xyz\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.False(results[1].Found);
        Assert.False(results[2].Found);
        Assert.Equal("\"abc\"", SliceToString(json, results[0]));
    }

    [Fact]
    public void ScanMessage_OnlyCorrelationIdPresent_FindsOnlyCorrelationSlice()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"correlationId\":\"corr-xyz\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.False(results[0].Found);
        Assert.True(results[1].Found);
        Assert.False(results[2].Found);
        Assert.Equal("\"corr-xyz\"", SliceToString(json, results[1]));
    }

    [Fact]
    public void ScanMessage_OnlyValuePresent_FindsOnlyValueSlice()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"value\":\"my-value\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.False(results[0].Found);
        Assert.False(results[1].Found);
        Assert.True(results[2].Found);
        Assert.Equal("\"my-value\"", SliceToString(json, results[2]));
    }

    [Fact]
    public void ScanMessage_NoMatchingKeys_FindsNothing()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"a\":\"1\",\"b\":\"2\",\"c\":\"3\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.False(results[0].Found);
        Assert.False(results[1].Found);
        Assert.False(results[2].Found);
    }

    [Fact]
    public void ScanMessage_EmptyJsonObject_FindsNothing()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.False(results[0].Found);
        Assert.False(results[1].Found);
        Assert.False(results[2].Found);
    }

    [Fact]
    public void ScanMessage_NestedPropertiesAtDepthGreaterThanOne_AreIgnored()
    {
        var scanner = CreateScanner;
        // key and correlationId are nested at depth 2, should be ignored
        var json = ToUtf8("{\"outer\":{\"key\":\"nested-key\",\"correlationId\":\"nested-corr\"},\"value\":\"top\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.False(results[0].Found);
        Assert.False(results[1].Found);
        Assert.True(results[2].Found);
        Assert.Equal("\"top\"", SliceToString(json, results[2]));
    }

    [Fact]
    public void ScanMessage_NestedValueInArray_IsIgnored()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"items\":[{\"key\":\"in-array\"}],\"value\":\"top\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.False(results[0].Found);
        Assert.True(results[2].Found);
    }

    [Fact]
    public void ScanMessage_DuplicateKeys_FirstOccurrenceWins()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":\"first\",\"key\":\"second\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.Equal("\"first\"", SliceToString(json, results[0]));
    }

    [Fact]
    public void ScanMessage_ResultsAlreadyFound_AreNotOverwritten()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":\"new-value\"}");
        var results = CreateResults();
        results[0] = new JsonSlice(0, 5); // pre-populate

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        // Should retain original slice, not be overwritten
        Assert.Equal(0, results[0].Start);
        Assert.Equal(5, results[0].End);
    }

    [Fact]
    public void ScanMessage_StringValue_SliceCoversFullQuotedValue()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":\"hello world\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.Equal("\"hello world\"", SliceToString(json, results[0]));
    }

    [Fact]
    public void ScanMessage_NumericValue_SliceCoversNumber()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":12345}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.Equal("12345", SliceToString(json, results[0]));
    }

    [Fact]
    public void ScanMessage_BooleanValue_SliceCoversBoolean()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":true}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.Equal("true", SliceToString(json, results[0]));
    }

    [Fact]
    public void ScanMessage_NullValue_SliceCoversNull()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":null}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.Equal("null", SliceToString(json, results[0]));
    }

    [Fact]
    public void ScanMessage_ObjectValue_SliceCoversWholeObject()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":{\"nested\":\"obj\"}}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.Equal("{\"nested\":\"obj\"}", SliceToString(json, results[0]));
    }

    [Fact]
    public void ScanMessage_ArrayValue_SliceCoversWholeArray()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":[1,2,3]}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.Equal("[1,2,3]", SliceToString(json, results[0]));
    }

    [Fact]
    public void ScanMessage_EmptyStringValue_IsFound()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":\"\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.Equal("\"\"", SliceToString(json, results[0]));
    }

    [Fact]
    public void ScanMessage_KeysWithDifferentCase_AreNotMatched()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"Key\":\"1\",\"CORRELATIONID\":\"2\",\"VALUE\":\"3\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.False(results[0].Found);
        Assert.False(results[1].Found);
        Assert.False(results[2].Found);
    }

    [Fact]
    public void ScanMessage_ExtraWhitespace_StillFindsKeys()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{  \"key\"  :  \"value\"  ,  \"correlationId\"  :  \"corr\"  }");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.True(results[1].Found);
        Assert.Equal("\"value\"", SliceToString(json, results[0]));
        Assert.Equal("\"corr\"", SliceToString(json, results[1]));
    }

    [Fact]
    public void ScanMessage_KeysInAnyOrder_AreFound()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"value\":\"v\",\"correlationId\":\"c\",\"key\":\"k\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.True(results[1].Found);
        Assert.True(results[2].Found);
        Assert.Equal("\"k\"", SliceToString(json, results[0]));
        Assert.Equal("\"c\"", SliceToString(json, results[1]));
        Assert.Equal("\"v\"", SliceToString(json, results[2]));
    }

    [Fact]
    public void ScanMessage_ValueIsLastPropertyInDocument_IsFound()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":\"k\",\"value\":\"last\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[2].Found);
        Assert.Equal("\"last\"", SliceToString(json, results[2]));
    }

    [Fact]
    public void ScanMessage_ValueIsFirstPropertyInDocument_IsFound()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"value\":\"first\",\"key\":\"k\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[2].Found);
        Assert.Equal("\"first\"", SliceToString(json, results[2]));
    }

    [Fact]
    public void ScanMessage_UnicodeEscapedValue_SliceIsCorrect()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":\"\\u0041\\u0042\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.Equal("\"\\u0041\\u0042\"", SliceToString(json, results[0]));
    }

    [Fact]
    public void ScanMessage_NestedArraysInsideValue_DoNotAffectSlice()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":[[1,2],[3,4]],\"correlationId\":\"c\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.Equal("[[1,2],[3,4]]", SliceToString(json, results[0]));
        Assert.True(results[1].Found);
    }

    [Fact]
    public void ScanMessage_DeeplyNestedObjectsInsideValue_SliceIsCorrect()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":{\"a\":{\"b\":{\"c\":[1,2,3]}}}}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.Equal("{\"a\":{\"b\":{\"c\":[1,2,3]}}}", SliceToString(json, results[0]));
    }

    [Fact]
    public void ScanMessage_DuplicateNestedKeyIgnored_TopLevelFound()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"inner\":{\"key\":\"nested\"},\"key\":\"top\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.Equal("\"top\"", SliceToString(json, results[0]));
    }

    [Fact]
    public void ScanMessage_LargeValue_SliceIsCorrect()
    {
        var scanner = CreateScanner;
        var largeValue = new string('x', 10_000);
        var json = ToUtf8("{\"key\":\"" + largeValue + "\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.Equal("\"" + largeValue + "\"", SliceToString(json, results[0]));
    }

    [Fact]
    public void ScanMessage_ThrowsOnMalformedJson()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":");
        var results = CreateResults();

        Assert.ThrowsAny<JsonException>(() => scanner.ScanMessage(json, results));
    }

    [Fact]
    public void ScanMessage_EmptyInput_Throws()
    {
        var scanner = CreateScanner;
        var results = CreateResults();

        Assert.ThrowsAny<JsonException>(() => scanner.ScanMessage([], results));
    }

    [Fact]
    public void ScanMessage_NonObjectRoot_ThrowsOrFindsNothing()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("[1,2,3]");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.False(results[0].Found);
        Assert.False(results[1].Found);
        Assert.False(results[2].Found);
    }

    [Fact]
    public void ScanMessage_KeysThatSharePrefix_AreNotConfused()
    {
        var scanner = CreateScanner;
        // "key" vs "keys" - these should not match each other
        var json = ToUtf8("{\"keys\":\"wrong\",\"key\":\"right\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.Equal("\"right\"", SliceToString(json, results[0]));
    }

    [Fact]
    public void ScanMessage_SliceIndicesAreWithinBounds()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":\"abc\",\"correlationId\":\"def\",\"value\":\"ghi\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        for (int i = 0; i < results.Length; i++)
        {
            Assert.True(results[i].Start >= 0);
            Assert.True(results[i].End > results[i].Start);
            Assert.True(results[i].End <= json.Length);
        }
    }

    [Fact]
    public void ScanMessage_SlicesDoNotOverlap()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":\"aaa\",\"correlationId\":\"bbb\",\"value\":\"ccc\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].End <= results[1].Start);
        Assert.True(results[1].End <= results[2].Start);
    }

    [Fact]
    public void ScanMessage_ValueFollowedByOtherProperties_SliceEndsBeforeNextProperty()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":\"abc\",\"other\":\"xyz\"}");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.Equal("\"abc\"", SliceToString(json, results[0]));
    }

    [Fact]
    public void ScanMessage_ValueWithTrailingWhitespaceInDocument_SliceIsExact()
    {
        var scanner = CreateScanner;
        var json = ToUtf8("{\"key\":\"abc\"   }   ");
        var results = CreateResults();

        scanner.ScanMessage(json, results);

        Assert.True(results[0].Found);
        Assert.Equal("\"abc\"", SliceToString(json, results[0]));
    }

    [Theory]
    [InlineData("{\"key\":\"v1\"}")]
    [InlineData("{\"key\":\"v1\",\"correlationId\":\"v2\"}")]
    [InlineData("{\"value\":\"v3\"}")]
    [InlineData("{\"key\":null,\"correlationId\":false,\"value\":0}")]
    [InlineData("{\"key\":{\"a\":1},\"correlationId\":[1,2],\"value\":\"s\"}")]
    public void ScanMessage_VariousJsonShapes_DoNotThrow(string json)
    {
        var scanner = CreateScanner;
        var bytes = ToUtf8(json);
        var results = CreateResults();

        var exception = Record.Exception(() => scanner.ScanMessage(bytes, results));

        Assert.Null(exception);
    }
}