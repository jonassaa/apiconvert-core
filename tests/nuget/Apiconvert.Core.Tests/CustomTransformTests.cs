using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Tests;

public sealed class CustomTransformTests
{
    [Fact]
    public void ApplyConversion_CustomTransform_UsesRegistryHandler()
    {
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["user.code"],
                    Source = new ValueSource
                    {
                        Type = "transform",
                        Path = "name",
                        CustomTransform = "reverse"
                    }
                }
            ]
        };

        var input = new Dictionary<string, object?> { ["name"] = "Ada" };
        var options = new ConversionOptions
        {
            TransformRegistry = new Dictionary<string, Func<object?, object?>>(StringComparer.Ordinal)
            {
                ["reverse"] = value => new string((value?.ToString() ?? string.Empty).Reverse().ToArray())
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules, options);

        Assert.Empty(result.Errors);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        var user = Assert.IsType<Dictionary<string, object?>>(output["user"]);
        Assert.Equal("adA", user["code"]);
    }

    [Fact]
    public void ApplyConversion_CustomTransform_MissingRegistryEntry_AddsError()
    {
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["user.code"],
                    Source = new ValueSource
                    {
                        Type = "transform",
                        Path = "name",
                        CustomTransform = "reverse"
                    }
                }
            ]
        };

        var input = new Dictionary<string, object?> { ["name"] = "Ada" };
        var result = ConversionEngine.ApplyConversion(input, rules, new ConversionOptions());

        Assert.Contains(result.Errors, error => error.Contains("custom transform 'reverse' is not registered", StringComparison.Ordinal));
    }
}
