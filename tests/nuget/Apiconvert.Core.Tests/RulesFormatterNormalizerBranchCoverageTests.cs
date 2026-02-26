using System.Text.Json.Nodes;
using Apiconvert.Core.Converters;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class RulesFormatterNormalizerBranchCoverageTests
{
    [Fact]
    public void FormatConversionRules_CanonicalizesConditionTrueFalseValues()
    {
        var rawRules = """
        {
          "rules": [
            {
              "kind": "field",
              "to": ["decision"],
              "source": {
                "type": "condition",
                "expression": "path(flag) eq true",
                "trueValue": "T",
                "falseValue": "F"
              }
            }
          ]
        }
        """;

        var formatted = ConversionEngine.FormatConversionRules(rawRules, pretty: false);
        var root = JsonNode.Parse(formatted)!.AsObject();
        var source = root["rules"]![0]!["source"]!.AsObject();

        Assert.Equal("T", source["trueValue"]!.GetValue<string>());
        Assert.Equal("F", source["falseValue"]!.GetValue<string>());
    }

    [Fact]
    public void NormalizeConversionRules_ReportsBranchAndElseIfMissingExpressions()
    {
        var rawRules = """
        {
          "rules": [
            {
              "kind": "branch",
              "then": [],
              "elseIf": [ { "then": [] } ],
              "else": []
            }
          ]
        }
        """;

        var rules = ConversionEngine.NormalizeConversionRules(rawRules);

        Assert.Contains(rules.ValidationErrors, error => error.Contains("rules[0]: expression is required", StringComparison.Ordinal));
        Assert.Contains(rules.ValidationErrors, error => error.Contains("rules[0].elseIf[0]: expression is required", StringComparison.Ordinal));
    }
}
