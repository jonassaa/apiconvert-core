using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class MappingExecutorCoverageTests
{
    [Fact]
    public void ApplyConversion_ConditionExpressions_CoverAliasesExistsInAndSuggestions()
    {
        var input = new Dictionary<string, object?>
        {
            ["score"] = 90,
            ["name"] = "Ada",
            ["flags"] = new List<object?> { "vip", "paid" },
            ["nested"] = new Dictionary<string, object?>
            {
                ["items"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["v"] = "x" },
                    new Dictionary<string, object?> { ["v"] = "y" }
                }
            }
        };

        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "branch",
                    Expression = "exists(path(name)) and path(score) gte 90",
                    Then =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["ok"],
                            Source = new ValueSource { Type = "constant", Value = "yes" }
                        }
                    ]
                },
                new RuleNode
                {
                    Kind = "branch",
                    Expression = "path(name) in ['Ada','Bob']",
                    Then =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["inList"],
                            Source = new ValueSource { Type = "constant", Value = "1" }
                        }
                    ]
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["indexed"],
                    Source = new ValueSource { Type = "path", Path = "nested.items[1].v" }
                },
                new RuleNode
                {
                    Kind = "branch",
                    Expression = "path(score) is 90",
                    Then = []
                }
            ]
        };

        var result = ConversionEngine.ApplyConversion(input, rules);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);

        Assert.Equal("yes", output["ok"]);
        Assert.Equal("1", Convert.ToString(output["inList"], System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("y", output["indexed"]);
        Assert.Contains(result.Errors, e => e.Contains("Did you mean 'eq' or '=='?", StringComparison.Ordinal));
    }

    [Fact]
    public void ApplyConversion_TransformsAndConditions_CoverFallbackBranches()
    {
        var input = new Dictionary<string, object?>
        {
            ["name"] = "Ada Lovelace",
            ["truthy"] = "yes",
            ["num"] = "7.5"
        };

        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["concat"],
                    Source = new ValueSource { Type = "transform", Transform = TransformType.Concat, Path = "const:Hello ,name" }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["splitLast"],
                    Source = new ValueSource
                    {
                        Type = "transform",
                        Transform = TransformType.Split,
                        Path = "name",
                        Separator = " ",
                        TokenIndex = -1,
                        TrimAfterSplit = true
                    }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["splitOut"],
                    Source = new ValueSource
                    {
                        Type = "transform",
                        Transform = TransformType.Split,
                        Path = "name",
                        Separator = " ",
                        TokenIndex = 99
                    }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["asBool"],
                    Source = new ValueSource { Type = "transform", Transform = TransformType.Boolean, Path = "truthy" }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["asNum"],
                    Source = new ValueSource { Type = "transform", Transform = TransformType.Number, Path = "num" }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["defaultTransform"],
                    Source = new ValueSource { Type = "transform", Path = "name" }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["conditionMatch"],
                    Source = new ValueSource
                    {
                        Type = "condition",
                        Expression = "path(name) eq 'Ada Lovelace'",
                        ConditionOutput = ConditionOutputMode.Match,
                        TrueValue = "1",
                        FalseValue = "0"
                    }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["unsupported"],
                    Source = new ValueSource { Type = "mystery" }
                }
            ]
        };

        var result = ConversionEngine.ApplyConversion(input, rules);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);

        Assert.Equal("HelloAda Lovelace", output["concat"]);
        Assert.Equal("Lovelace", output["splitLast"]);
        Assert.Null(output["splitOut"]);
        Assert.Equal(true, output["asBool"]);
        Assert.Equal(7.5d, output["asNum"]);
        Assert.Equal("ada lovelace", output["defaultTransform"]);
        Assert.Equal(true, output["conditionMatch"]);
        Assert.Contains(result.Errors, error => error.Contains("unsupported source type 'mystery'", StringComparison.Ordinal));
    }

    [Fact]
    public void ApplyConversion_CustomTransformFailure_AndConditionRecursionLimit_AreReported()
    {
        ValueSource source = new()
        {
            Type = "constant",
            Value = "end"
        };

        for (var i = 0; i < 70; i++)
        {
            source = new ValueSource
            {
                Type = "condition",
                Expression = "true",
                TrueSource = source
            };
        }

        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["custom"],
                    Source = new ValueSource
                    {
                        Type = "transform",
                        Path = "name",
                        CustomTransform = "explode"
                    }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["deep"],
                    Source = source
                }
            ]
        };

        var options = new ConversionOptions
        {
            TransformRegistry = new Dictionary<string, Func<object?, object?>>(StringComparer.Ordinal)
            {
                ["explode"] = _ => throw new InvalidOperationException("boom")
            }
        };

        var result = ConversionEngine.ApplyConversion(new Dictionary<string, object?> { ["name"] = "Ada" }, rules, options);

        Assert.Contains(result.Errors, error => error.Contains("custom transform 'explode' failed", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => error.Contains("condition/source recursion limit exceeded", StringComparison.Ordinal));
    }
}
