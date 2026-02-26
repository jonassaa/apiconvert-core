using System.Text.Json.Nodes;
using Apiconvert.Core.Converters;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class RulesFormatterTransformCoverageTests
{
    [Fact]
    public void FormatConversionRules_CanonicalizesAllTransformAliases()
    {
        var rawRules = """
        {
          "rules": [
            { "kind": "field", "to": ["lower"], "source": { "type": "transform", "path": "x", "transform": "toLowerCase" } },
            { "kind": "field", "to": ["upper"], "source": { "type": "transform", "path": "x", "transform": "toUpperCase" } },
            { "kind": "field", "to": ["num"], "source": { "type": "transform", "path": "x", "transform": "number" } },
            { "kind": "field", "to": ["bool"], "source": { "type": "transform", "path": "x", "transform": "boolean" } },
            { "kind": "field", "to": ["concat"], "source": { "type": "transform", "path": "x", "transform": "concat" } },
            { "kind": "field", "to": ["split"], "source": { "type": "transform", "path": "x", "transform": "split" } }
          ]
        }
        """;

        var text = ConversionEngine.FormatConversionRules(rawRules, pretty: false);
        var root = JsonNode.Parse(text)!.AsObject();
        var rules = root["rules"]!.AsArray();

        Assert.Equal("toLowerCase", rules[0]!["source"]!["transform"]!.GetValue<string>());
        Assert.Equal("toUpperCase", rules[1]!["source"]!["transform"]!.GetValue<string>());
        Assert.Equal("number", rules[2]!["source"]!["transform"]!.GetValue<string>());
        Assert.Equal("boolean", rules[3]!["source"]!["transform"]!.GetValue<string>());
        Assert.Equal("concat", rules[4]!["source"]!["transform"]!.GetValue<string>());
        Assert.Equal("split", rules[5]!["source"]!["transform"]!.GetValue<string>());
    }
}
