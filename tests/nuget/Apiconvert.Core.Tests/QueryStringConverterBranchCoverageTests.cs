using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class QueryStringConverterBranchCoverageTests
{
    [Fact]
    public void ParsePayload_Query_HandlesEmptyBracketKeyAndRepeatedIndexAsList()
    {
        var (value, error) = ConversionEngine.ParsePayload("[]=v&a[0]=x&a[0]=y&name=A&name=B&name=C", DataFormat.Query);

        Assert.Null(error);
        var obj = Assert.IsType<Dictionary<string, object?>>(value);

        Assert.Equal("v", obj["[]"]);

        var a = Assert.IsType<List<object?>>(obj["a"]);
        var first = Assert.IsType<List<object?>>(a[0]);
        Assert.Equal("x", first[0]);
        Assert.Equal("y", first[1]);

        var names = Assert.IsType<List<object?>>(obj["name"]);
        Assert.Equal(3, names.Count);
    }
}
