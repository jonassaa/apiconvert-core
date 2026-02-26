using System.Text;
using System.Text.Json.Nodes;
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class PayloadAndJsonConverterTests
{
    [Fact]
    public void ParsePayload_Stream_RespectsLeaveOpen()
    {
        using var keepOpen = new MemoryStream(Encoding.UTF8.GetBytes("{\"id\":1}"));
        var parsed = ConversionEngine.ParsePayload(keepOpen, DataFormat.Json, leaveOpen: true);
        Assert.Null(parsed.Error);
        Assert.True(keepOpen.CanRead);

        var closeStream = new MemoryStream(Encoding.UTF8.GetBytes("{\"id\":1}"));
        var parsedClosed = ConversionEngine.ParsePayload(closeStream, DataFormat.Json, leaveOpen: false);
        Assert.Null(parsedClosed.Error);
        Assert.Throws<ObjectDisposedException>(() => _ = closeStream.Length);
    }

    [Fact]
    public void ParsePayload_JsonNode_RejectsNonJsonFormat()
    {
        var (value, error) = ConversionEngine.ParsePayload(JsonNode.Parse("{\"a\":1}"), DataFormat.Xml);

        Assert.Null(value);
        Assert.Equal("JsonNode input is only supported for DataFormat.Json.", error);
    }

    [Fact]
    public void ParsePayload_JsonNodeNull_ReturnsNullValue()
    {
        var (value, error) = ConversionEngine.ParsePayload((JsonNode?)null, DataFormat.Json);

        Assert.Null(error);
        Assert.Null(value);
    }

    [Fact]
    public void FormatPayload_Stream_ThrowsForNullStream()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ConversionEngine.FormatPayload(new Dictionary<string, object?>(), DataFormat.Json, null!, pretty: false));
    }

    [Fact]
    public void ParsePayload_Json_ConvertsPrimitiveKinds()
    {
        var (value, error) = ConversionEngine.ParsePayload(
            """
            {
              "whole": 12,
              "fraction": 2.5,
              "flag": true,
              "none": null,
              "arr": [1, "x", false]
            }
            """,
            DataFormat.Json);

        Assert.Null(error);
        var obj = Assert.IsType<Dictionary<string, object?>>(value);
        Assert.True(obj["whole"] is long or double);
        Assert.IsType<double>(obj["fraction"]);
        Assert.Equal(true, obj["flag"]);
        Assert.Null(obj["none"]);

        var arr = Assert.IsType<List<object?>>(obj["arr"]);
        Assert.True(arr[0] is long or double);
        Assert.Equal("x", arr[1]);
        Assert.Equal(false, arr[2]);
    }
}
