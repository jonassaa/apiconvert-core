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

    [Fact]
    public async Task StreamConversionAsync_Ndjson_ConvertsEachLine()
    {
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["user.id"],
                    Source = new ValueSource { Type = "path", Path = "id" }
                }
            ]
        };

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""
            {"id":"a1"}
            {"id":"b2"}
            """));
        var outputs = new List<string>();

        await foreach (var result in ConversionEngine.StreamConversionAsync(
                           stream,
                           rules,
                           new StreamConversionOptions
                           {
                               InputKind = StreamInputKind.Ndjson,
                               ErrorMode = StreamErrorMode.ContinueWithReport
                           }))
        {
            Assert.Empty(result.Errors);
            var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
            var user = Assert.IsType<Dictionary<string, object?>>(output["user"]);
            outputs.Add(Assert.IsType<string>(user["id"]));
        }

        Assert.Equal(["a1", "b2"], outputs);
    }

    [Fact]
    public async Task StreamConversionAsync_QueryLines_ConvertsEachLine()
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

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""
            name=Ada
            name=Bob
            """));
        var outputs = new List<string>();

        await foreach (var result in ConversionEngine.StreamConversionAsync(
                           stream,
                           rules,
                           new StreamConversionOptions
                           {
                               InputKind = StreamInputKind.QueryLines,
                               ErrorMode = StreamErrorMode.ContinueWithReport
                           }))
        {
            Assert.Empty(result.Errors);
            var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
            var user = Assert.IsType<Dictionary<string, object?>>(output["user"]);
            outputs.Add(Assert.IsType<string>(user["name"]));
        }

        Assert.Equal(["Ada", "Bob"], outputs);
    }

    [Fact]
    public async Task StreamConversionAsync_XmlElements_ConvertsMatchedNodes()
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

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""
            <customers>
              <customer><name>Ada</name></customer>
              <customer><name>Bob</name></customer>
            </customers>
            """));

        var outputs = new List<string>();
        await foreach (var result in ConversionEngine.StreamConversionAsync(
                           stream,
                           rules,
                           new StreamConversionOptions
                           {
                               InputKind = StreamInputKind.XmlElements,
                               XmlItemPath = "customers.customer",
                               ErrorMode = StreamErrorMode.ContinueWithReport
                           }))
        {
            Assert.Empty(result.Errors);
            var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
            var user = Assert.IsType<Dictionary<string, object?>>(output["user"]);
            outputs.Add(Assert.IsType<string>(user["name"]));
        }

        Assert.Equal(["Ada", "Bob"], outputs);
    }

    [Fact]
    public async Task StreamConversionAsync_FailFast_ThrowsOnItemError()
    {
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["user.id"],
                    Source = new ValueSource { Type = "path", Path = "id" }
                }
            ]
        };

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""
            {"id":"ok"}
            not-json
            """));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in ConversionEngine.StreamConversionAsync(
                               stream,
                               rules,
                               new StreamConversionOptions
                               {
                                   InputKind = StreamInputKind.Ndjson,
                                   ErrorMode = StreamErrorMode.FailFast
                               }))
            {
            }
        });

        Assert.Contains("failed to parse NDJSON line", error.Message);
    }

    [Fact]
    public async Task StreamConversionAsync_ContinueWithReport_ReturnsItemError()
    {
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["user.id"],
                    Source = new ValueSource { Type = "path", Path = "id" }
                }
            ]
        };

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""
            {"id":"ok"}
            not-json
            {"id":"next"}
            """));

        var results = new List<ConversionResult>();
        await foreach (var result in ConversionEngine.StreamConversionAsync(
                           stream,
                           rules,
                           new StreamConversionOptions
                           {
                               InputKind = StreamInputKind.Ndjson,
                               ErrorMode = StreamErrorMode.ContinueWithReport
                           }))
        {
            results.Add(result);
        }

        Assert.Equal(3, results.Count);
        Assert.Empty(results[0].Errors);
        Assert.Single(results[1].Errors);
        Assert.Contains("stream[1]", results[1].Errors[0]);
        Assert.Empty(results[2].Errors);
    }
}
