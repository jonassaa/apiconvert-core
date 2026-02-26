using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class RulesNormalizerCoverageTests
{
    [Fact]
    public void NormalizeConversionRules_ReportsUnsupportedInputTypes()
    {
        var fromInt = ConversionEngine.NormalizeConversionRules(123);
        Assert.Contains(fromInt.ValidationErrors, e => e.Contains("unsupported rules input type", StringComparison.Ordinal));

        var fromArrayJson = ConversionEngine.NormalizeConversionRules("[]");
        Assert.Contains(fromArrayJson.ValidationErrors, e => e.Contains("expected JSON object", StringComparison.Ordinal));
    }

    [Fact]
    public void NormalizeConversionRules_ReportsFragmentErrorsAndUnknownTransformAlias()
    {
        var rules = new ConversionRules
        {
            Fragments = new Dictionary<string, RuleNode>
            {
                ["a"] = new RuleNode { Use = "b" },
                ["b"] = new RuleNode { Use = "a" }
            },
            Rules =
            [
                new RuleNode { Use = "missing" },
                new RuleNode { Use = "a" },
                new RuleNode { Kind = "map", Entries = [] },
                new RuleNode { Kind = "field", To = System.Text.Json.JsonDocument.Parse("[\"x\"]").RootElement, From = "name", As = "unknownTransform" }
            ]
        };

        var normalized = ConversionEngine.NormalizeConversionRules(rules);

        Assert.Contains(normalized.ValidationErrors, e => e.Contains("unknown fragment 'missing'", StringComparison.Ordinal));
        Assert.Contains(normalized.ValidationErrors, e => e.Contains("introduces a cycle", StringComparison.Ordinal));
        Assert.Contains(normalized.ValidationErrors, e => e.Contains("entries: is required for map rule", StringComparison.Ordinal));
        Assert.Contains(normalized.ValidationErrors, e => e.Contains("unsupported transform alias 'unknownTransform'", StringComparison.Ordinal));
    }
}
