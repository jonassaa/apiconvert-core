using System.Text.Json.Nodes;
using Apiconvert.Core.Converters;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class RulesFormatterCoverageTests
{
    [Fact]
    public void FormatConversionRules_CanonicalizesArrayAndBranchRules()
    {
        var rawRules = """
        {
          "inputFormat": "xml",
          "outputFormat": "query",
          "rules": [
            {
              "kind": "array",
              "inputPath": "orders",
              "outputPaths": ["items"],
              "coerceSingle": true,
              "itemRules": [
                { "kind": "field", "from": "id", "to": ["item.id"] }
              ]
            },
            {
              "kind": "branch",
              "expression": "path(total) > 0",
              "then": [
                { "kind": "field", "to": ["status"], "const": "ok" }
              ],
              "elseIf": [
                {
                  "expression": "path(total) == 0",
                  "then": [
                    { "kind": "field", "to": ["status"], "const": "empty" }
                  ]
                }
              ],
              "else": [
                { "kind": "field", "to": ["status"], "const": "bad" }
              ]
            }
          ]
        }
        """;

        var formatted = ConversionEngine.FormatConversionRules(rawRules, pretty: false);
        var node = JsonNode.Parse(formatted)!.AsObject();

        Assert.Equal("xml", node["inputFormat"]!.GetValue<string>());
        Assert.Equal("query", node["outputFormat"]!.GetValue<string>());

        var rules = node["rules"]!.AsArray();
        var arrayRule = rules[0]!.AsObject();
        Assert.Equal("array", arrayRule["kind"]!.GetValue<string>());
        Assert.True(arrayRule["coerceSingle"]!.GetValue<bool>());
        Assert.Equal("orders", arrayRule["inputPath"]!.GetValue<string>());

        var branchRule = rules[1]!.AsObject();
        Assert.Equal("branch", branchRule["kind"]!.GetValue<string>());
        Assert.Equal("path(total) > 0", branchRule["expression"]!.GetValue<string>());
        Assert.Single(branchRule["elseIf"]!.AsArray());
        Assert.Single(branchRule["else"]!.AsArray());

        Assert.DoesNotContain('\n', formatted);
    }

    [Fact]
    public void FormatConversionRules_CanonicalizesSourceDefaultsAndElseIfBranches()
    {
        var rawRules = """
        {
          "rules": [
            {
              "kind": "field",
              "to": ["merged"],
              "source": {
                "type": "merge",
                "paths": ["a", "b"]
              }
            },
            {
              "kind": "field",
              "to": ["decision"],
              "source": {
                "type": "condition",
                "expression": "path(flag) = true",
                "trueSource": { "type": "constant", "value": "yes" },
                "falseSource": { "type": "constant", "value": "no" },
                "elseIf": [
                  {
                    "expression": "path(flag) = null",
                    "value": "unknown"
                  }
                ]
              }
            },
            {
              "kind": "field",
              "to": ["normalized"],
              "source": {
                "type": "transform",
                "path": "name"
              }
            }
          ]
        }
        """;

        var formatted = ConversionEngine.FormatConversionRules(rawRules, pretty: true);
        var root = JsonNode.Parse(formatted)!.AsObject();
        var rules = root["rules"]!.AsArray();

        var mergeSource = rules[0]!["source"]!.AsObject();
        Assert.Equal("concat", mergeSource["mergeMode"]!.GetValue<string>());

        var conditionSource = rules[1]!["source"]!.AsObject();
        Assert.Equal("branch", conditionSource["conditionOutput"]!.GetValue<string>());
        var elseIf = conditionSource["elseIf"]!.AsArray();
        Assert.Single(elseIf);
        Assert.Equal("unknown", elseIf[0]!["value"]!.GetValue<string>());

        var transformSource = rules[2]!["source"]!.AsObject();
        Assert.Equal("toLowerCase", transformSource["transform"]!.GetValue<string>());
    }
}
