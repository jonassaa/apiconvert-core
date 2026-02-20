using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Tests;

public sealed class ConversionPlanCacheTests
{
    [Fact]
    public void ComputeRulesCacheKey_IsStable_ForEquivalentRules()
    {
        const string rawRules = """
        {
          "inputFormat": "json",
          "outputFormat": "json",
          "rules": [
            {
              "kind": "field",
              "outputPaths": ["user.name"],
              "source": { "type": "path", "path": "name" }
            }
          ]
        }
        """;

        var key1 = ConversionEngine.ComputeRulesCacheKey(rawRules);
        var key2 = ConversionEngine.ComputeRulesCacheKey(rawRules);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void CompileConversionPlan_ExposesCacheKey_AndMatchesDirectApply()
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

        var plan = ConversionEngine.CompileConversionPlan(rules);
        var cacheKey = ConversionEngine.ComputeRulesCacheKey(rules);

        Assert.Equal(cacheKey, plan.CacheKey);

        var input = new Dictionary<string, object?> { ["name"] = "Ada" };
        var viaPlan = plan.Apply(input);
        var direct = ConversionEngine.ApplyConversion(input, rules);

        Assert.Equal(
            ConversionEngine.FormatPayload(direct.Output, DataFormat.Json, pretty: false),
            ConversionEngine.FormatPayload(viaPlan.Output, DataFormat.Json, pretty: false));
        Assert.Equal(direct.Errors, viaPlan.Errors);
        Assert.Equal(direct.Warnings, viaPlan.Warnings);
    }
}
