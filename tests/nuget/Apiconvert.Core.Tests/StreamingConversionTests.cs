using System.Text;
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Tests;

public sealed class StreamingConversionTests
{
    [Fact]
    public async Task StreamJsonArrayConversionAsync_ConvertsEachItem()
    {
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["user.name"],
                    Source = new ValueSource { Type = "path", Path = "name" }
                }
            ]
        };

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""[{"name":"Ada"},{"name":"Bob"}]"""));
        var outputs = new List<string>();

        await foreach (var result in ConversionEngine.StreamJsonArrayConversionAsync(stream, rules))
        {
            Assert.Empty(result.Errors);
            var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
            var user = Assert.IsType<Dictionary<string, object?>>(output["user"]);
            outputs.Add(Assert.IsType<string>(user["name"]));
        }

        Assert.Equal(["Ada", "Bob"], outputs);
    }
}
