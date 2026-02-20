using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Tests;

public sealed class ConversionProfileTests
{
    [Fact]
    public void ProfileConversionPlan_ReturnsExpectedRunCounts()
    {
        var rules = new ConversionRules
        {
            InputFormat = DataFormat.Json,
            OutputFormat = DataFormat.Json,
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

        var report = ConversionEngine.ProfileConversionPlan(
            rules,
            inputs: [
                new Dictionary<string, object?> { ["name"] = "Ada" },
                new Dictionary<string, object?> { ["name"] = "Lin" }
            ],
            options: new ConversionProfileOptions { Iterations = 3, WarmupIterations = 1 });

        Assert.Equal(6, report.TotalRuns);
        Assert.Equal(3, report.Iterations);
        Assert.Equal(1, report.WarmupIterations);
        Assert.NotEmpty(report.PlanCacheKey);
        Assert.True(report.LatencyMs.P50 >= 0);
    }
}
