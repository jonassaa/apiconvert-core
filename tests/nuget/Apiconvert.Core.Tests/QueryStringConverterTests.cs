using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class QueryStringConverterTests
{
    [Fact]
    public void ParsePayload_Query_DecodesDuplicatesIndicesAndPlus()
    {
        var (value, error) = ConversionEngine.ParsePayload(
            "?name=Ada+Lovelace&name=Grace&items[0].id=1&items[1].id=2",
            DataFormat.Query);

        Assert.Null(error);
        var obj = Assert.IsType<Dictionary<string, object?>>(value);

        var name = Assert.IsType<List<object?>>(obj["name"]);
        Assert.Equal("Ada Lovelace", name[0]);
        Assert.Equal("Grace", name[1]);

        var items = Assert.IsType<List<object?>>(obj["items"]);
        var first = Assert.IsType<Dictionary<string, object?>>(items[0]);
        var second = Assert.IsType<Dictionary<string, object?>>(items[1]);
        Assert.Equal("1", first["id"]);
        Assert.Equal("2", second["id"]);
    }

    [Fact]
    public void FormatPayload_Query_SerializesNestedValuesDeterministically()
    {
        var payload = new Dictionary<string, object?>
        {
            ["tags"] = new List<object?>
            {
                "a",
                null,
                new Dictionary<string, object?> { ["z"] = "1" }
            },
            ["meta"] = new Dictionary<string, object?>
            {
                ["b"] = "2",
                ["a"] = "1"
            }
        };

        var query = ConversionEngine.FormatPayload(payload, DataFormat.Query, pretty: false);

        Assert.Equal("meta.a=1&meta.b=2&tags=a&tags=&tags=%7B%22z%22%3A%221%22%7D", query);
    }

    [Fact]
    public void FormatPayload_Query_ThrowsWhenOutputIsNotObject()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            ConversionEngine.FormatPayload(new List<object?> { "x" }, DataFormat.Query, pretty: false));

        Assert.Contains("Query output must be an object", error.Message, StringComparison.Ordinal);
    }
}
