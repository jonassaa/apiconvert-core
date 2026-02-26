using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class QueryStringConverterEdgeTests
{
    [Fact]
    public void ParsePayload_Query_CoversNestedArrayAndRepeatedIndices()
    {
        var (value, error) = ConversionEngine.ParsePayload(
            "items[0]=a&items[0]=b&meta.values[0]=x&meta.values[1]=y&plain",
            DataFormat.Query);

        Assert.Null(error);
        var obj = Assert.IsType<Dictionary<string, object?>>(value);

        var items = Assert.IsType<List<object?>>(obj["items"]);
        var first = Assert.IsType<List<object?>>(items[0]);
        Assert.Equal("a", first[0]);
        Assert.Equal("b", first[1]);

        var meta = Assert.IsType<Dictionary<string, object?>>(obj["meta"]);
        var values = Assert.IsType<List<object?>>(meta["values"]);
        Assert.Equal("x", values[0]);
        Assert.Equal("y", values[1]);
        Assert.Equal(string.Empty, obj["plain"]);
    }

    [Fact]
    public void FormatPayload_Query_HandlesNullsAndEmptyTopLevelKey()
    {
        var payload = new Dictionary<string, object?>
        {
            [""] = "ignored",
            ["a"] = null,
            ["list"] = new List<object?> { null, "x" }
        };

        var text = ConversionEngine.FormatPayload(payload, DataFormat.Query, pretty: false);

        Assert.Equal("a=&list=&list=x", text);
    }
}
