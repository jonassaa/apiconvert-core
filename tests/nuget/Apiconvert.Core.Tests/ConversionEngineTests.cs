using System.Text;
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class ConversionEngineTests
{
    [Fact]
    public void NormalizeConversionRules_ParsesRulesArray()
    {
        var json = """
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

        var rules = ConversionEngine.NormalizeConversionRules(json);

        Assert.Single(rules.Rules);
        Assert.Equal("field", rules.Rules[0].Kind);
        Assert.Empty(rules.ValidationErrors);
    }

    [Fact]
    public void NormalizeConversionRulesStrict_InvalidJson_Throws()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            ConversionEngine.NormalizeConversionRulesStrict("{"));

        Assert.Contains("invalid JSON payload", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NormalizeConversionRulesStrict_InvalidRule_Throws()
    {
        var json = """
        {
          "rules": [
            {
              "kind": "field",
              "source": { "type": "path", "path": "name" }
            }
          ]
        }
        """;

        var error = Assert.Throws<InvalidOperationException>(() =>
            ConversionEngine.NormalizeConversionRulesStrict(json));

        Assert.Contains("outputPaths is required", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NormalizeConversionRules_RootOutputPath_AddsValidationError()
    {
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["$"],
                    Source = new ValueSource { Type = "constant", Value = "ok" }
                }
            ]
        };

        var normalized = ConversionEngine.NormalizeConversionRules(rules);

        Assert.Contains(normalized.ValidationErrors, error => error.Contains("output path '$' is not supported", StringComparison.Ordinal));
    }

    [Fact]
    public void ApplyConversion_ExecutesBranchThenElseIfElse()
    {
        var input = new Dictionary<string, object?>
        {
            ["score"] = 72d
        };

        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "branch",
                    Expression = "path(score) >= 90",
                    Then =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["grade"],
                            Source = new ValueSource { Type = "constant", Value = "A" }
                        }
                    ],
                    ElseIf =
                    [
                        new BranchElseIfRule
                        {
                            Expression = "path(score) >= 80",
                            Then =
                            [
                                new RuleNode
                                {
                                    Kind = "field",
                                    OutputPaths = ["grade"],
                                    Source = new ValueSource { Type = "constant", Value = "B" }
                                }
                            ]
                        },
                        new BranchElseIfRule
                        {
                            Expression = "path(score) >= 70",
                            Then =
                            [
                                new RuleNode
                                {
                                    Kind = "field",
                                    OutputPaths = ["grade"],
                                    Source = new ValueSource { Type = "constant", Value = "C" }
                                }
                            ]
                        }
                    ],
                    Else =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["grade"],
                            Source = new ValueSource { Type = "constant", Value = "F" }
                        }
                    ]
                }
            ]
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        Assert.Empty(result.Errors);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        Assert.Equal("C", output["grade"]);
    }

    [Fact]
    public void ApplyConversion_BranchInsideArrayItemRules_UsesRootAndItemScope()
    {
        var input = new Dictionary<string, object?>
        {
            ["meta"] = new Dictionary<string, object?> { ["source"] = "api" },
            ["items"] = new List<object?>
            {
                new Dictionary<string, object?> { ["qty"] = 1d },
                new Dictionary<string, object?> { ["qty"] = 2d }
            }
        };

        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "array",
                    InputPath = "items",
                    OutputPaths = ["items"],
                    ItemRules =
                    [
                        new RuleNode
                        {
                            Kind = "branch",
                            Expression = "path($.meta.source) == 'api' && path(qty) >= 2",
                            Then =
                            [
                                new RuleNode
                                {
                                    Kind = "field",
                                    OutputPaths = ["priority"],
                                    Source = new ValueSource { Type = "constant", Value = "high" }
                                }
                            ],
                            Else =
                            [
                                new RuleNode
                                {
                                    Kind = "field",
                                    OutputPaths = ["priority"],
                                    Source = new ValueSource { Type = "constant", Value = "normal" }
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        Assert.Empty(result.Errors);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        var items = Assert.IsType<List<object?>>(output["items"]);
        var first = Assert.IsType<Dictionary<string, object?>>(items[0]);
        var second = Assert.IsType<Dictionary<string, object?>>(items[1]);
        Assert.Equal("normal", first["priority"]);
        Assert.Equal("high", second["priority"]);
    }

    [Fact]
    public void ApplyConversion_ConditionSourceStillWorks()
    {
        var input = new Dictionary<string, object?> { ["flag"] = true };
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["status"],
                    Source = new ValueSource
                    {
                        Type = "condition",
                        Expression = "path(flag) == true",
                        TrueValue = "enabled",
                        FalseValue = "disabled"
                    }
                }
            ]
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        Assert.Empty(result.Errors);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        Assert.Equal("enabled", output["status"]);
    }

    [Fact]
    public void ApplyConversion_BranchNumericEqualityAcrossTypes_Matches()
    {
        var input = new Dictionary<string, object?> { ["score"] = 1L };
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "branch",
                    Expression = "path(score) == 1.0",
                    Then =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["match"],
                            Source = new ValueSource { Type = "constant", Value = "yes" }
                        }
                    ],
                    Else =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["match"],
                            Source = new ValueSource { Type = "constant", Value = "no" }
                        }
                    ]
                }
            ]
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        Assert.Empty(result.Errors);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        Assert.Equal("yes", output["match"]);
    }

    [Fact]
    public void ApplyConversion_InOperatorNumericEqualityAcrossTypes_Matches()
    {
        var input = new Dictionary<string, object?> { ["score"] = 1L };
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "branch",
                    Expression = "path(score) in [1.0, 2.0]",
                    Then =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["match"],
                            Source = new ValueSource { Type = "constant", Value = "yes" }
                        }
                    ],
                    Else =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["match"],
                            Source = new ValueSource { Type = "constant", Value = "no" }
                        }
                    ]
                }
            ]
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        Assert.Empty(result.Errors);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        Assert.Equal("yes", output["match"]);
    }

    [Fact]
    public void ApplyConversion_DefaultCollisionPolicy_LastWriteWins()
    {
        var result = ConvertWithCollisionRules();

        Assert.Empty(result.Errors);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        Assert.Equal("third", output["name"]);
    }

    [Fact]
    public void ApplyConversion_FirstWriteWins_KeepsFirstValue()
    {
        var result = ConvertWithCollisionRules(new ConversionOptions
        {
            CollisionPolicy = OutputCollisionPolicy.FirstWriteWins
        });

        Assert.Empty(result.Errors);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        Assert.Equal("first", output["name"]);
    }

    [Fact]
    public void ApplyConversion_ErrorCollisionPolicy_ReportsAllCollisions()
    {
        var result = ConvertWithCollisionRules(new ConversionOptions
        {
            CollisionPolicy = OutputCollisionPolicy.Error
        });

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, error => error.Contains("rules[1]", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => error.Contains("rules[2]", StringComparison.Ordinal));
        Assert.All(result.Errors, error => Assert.Contains("already written by rules[0]", error, StringComparison.Ordinal));

        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        Assert.Equal("first", output["name"]);
    }

    [Fact]
    public void ApplyConversion_ExplainDisabled_ReturnsEmptyTrace()
    {
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["name"],
                    Source = new ValueSource { Type = "constant", Value = "Ada" }
                }
            ]
        };

        var result = ConversionEngine.ApplyConversion(new Dictionary<string, object?>(), rules);

        Assert.Empty(result.Errors);
        Assert.Empty(result.Trace);
    }

    [Fact]
    public void ApplyConversion_ExplainEnabled_EmitsDeterministicTraceTimeline()
    {
        var input = new Dictionary<string, object?>
        {
            ["name"] = "Ada",
            ["score"] = 72d,
            ["items"] = new List<object?>
            {
                new Dictionary<string, object?> { ["id"] = "A1" },
                new Dictionary<string, object?> { ["id"] = "B2" }
            }
        };

        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["profile.name"],
                    Source = new ValueSource { Type = "path", Path = "name" }
                },
                new RuleNode
                {
                    Kind = "branch",
                    Expression = "path(score) >= 80",
                    Then =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["profile.grade"],
                            Source = new ValueSource { Type = "constant", Value = "B" }
                        }
                    ],
                    Else =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["profile.grade"],
                            Source = new ValueSource { Type = "constant", Value = "C" }
                        }
                    ]
                },
                new RuleNode
                {
                    Kind = "array",
                    InputPath = "items",
                    OutputPaths = ["lines"],
                    ItemRules =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["id"],
                            Source = new ValueSource { Type = "path", Path = "id" }
                        }
                    ]
                }
            ]
        };

        var result = ConversionEngine.ApplyConversion(input, rules, new ConversionOptions { Explain = true });

        Assert.Empty(result.Errors);
        Assert.Equal(6, result.Trace.Count);

        Assert.Equal("rules[0]", result.Trace[0].RulePath);
        Assert.Equal("field", result.Trace[0].RuleKind);
        Assert.Equal("applied", result.Trace[0].Decision);

        Assert.Equal("rules[1]", result.Trace[1].RulePath);
        Assert.Equal("branch", result.Trace[1].RuleKind);
        Assert.Equal("else", result.Trace[1].Decision);

        Assert.Equal("rules[1].else[0]", result.Trace[2].RulePath);
        Assert.Equal("field", result.Trace[2].RuleKind);
        Assert.Equal("applied", result.Trace[2].Decision);

        Assert.Equal("rules[2].itemRules[0]", result.Trace[3].RulePath);
        Assert.Equal("field", result.Trace[3].RuleKind);
        Assert.Equal("applied", result.Trace[3].Decision);

        Assert.Equal("rules[2].itemRules[0]", result.Trace[4].RulePath);
        Assert.Equal("field", result.Trace[4].RuleKind);
        Assert.Equal("applied", result.Trace[4].Decision);

        Assert.Equal("rules[2]", result.Trace[5].RulePath);
        Assert.Equal("array", result.Trace[5].RuleKind);
        Assert.Equal("mapped", result.Trace[5].Decision);
        Assert.Equal(["lines"], result.Trace[5].OutputPaths);
    }

    [Fact]
    public void CompileConversionPlan_ReusesNormalizedRules()
    {
        var json = """
        {
          "rules": [
            {
              "kind": "field",
              "outputPaths": ["user.name"],
              "source": { "type": "path", "path": "name" }
            }
          ]
        }
        """;
        var plan = ConversionEngine.CompileConversionPlanStrict(json);

        var first = ConversionEngine.ApplyConversion(
            new Dictionary<string, object?> { ["name"] = "Ada" },
            plan);
        var second = plan.Apply(new Dictionary<string, object?> { ["name"] = "Lin" });

        Assert.Empty(first.Errors);
        Assert.Empty(second.Errors);
        var firstOutput = Assert.IsType<Dictionary<string, object?>>(first.Output);
        var secondOutput = Assert.IsType<Dictionary<string, object?>>(second.Output);
        var firstUser = Assert.IsType<Dictionary<string, object?>>(firstOutput["user"]);
        var secondUser = Assert.IsType<Dictionary<string, object?>>(secondOutput["user"]);
        Assert.Equal("Ada", firstUser["name"]);
        Assert.Equal("Lin", secondUser["name"]);
    }

    [Fact]
    public void ApplyConversion_RuleRecursionDepthExceeded_AddsError()
    {
        var root = new RuleNode { Kind = "branch", Expression = "true", Then = [] };
        var cursor = root;
        for (var index = 0; index < 70; index++)
        {
            var child = new RuleNode { Kind = "branch", Expression = "true", Then = [] };
            cursor.Then.Add(child);
            cursor = child;
        }

        var rules = new ConversionRules { Rules = [root] };

        var result = ConversionEngine.ApplyConversion(new Dictionary<string, object?>(), rules);

        Assert.Contains(result.Errors, error => error.Contains("rule recursion limit exceeded"));
    }

    [Fact]
    public void ApplyConversion_BranchExpressionUnknownOperator_ErrorIsActionable()
    {
        var error = GetBranchExpressionError("path(name) is 'nora'");

        Assert.Contains("invalid branch expression", error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("position 11", error, StringComparison.Ordinal);
        Assert.Contains("Expected comparison operator", error, StringComparison.Ordinal);
        Assert.Contains("found 'is'", error, StringComparison.Ordinal);
        Assert.Contains("Did you mean 'eq' or '=='?", error, StringComparison.Ordinal);
        Assert.Contains("^^", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyConversion_BranchExpressionMissingOperand_ErrorIsActionable()
    {
        var error = GetBranchExpressionError("path(name) ==");

        Assert.Contains("position 13", error, StringComparison.Ordinal);
        Assert.Contains("Expected right-hand operand", error, StringComparison.Ordinal);
        Assert.Contains("end of expression", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyConversion_BranchExpressionUnclosedGrouping_ErrorIsActionable()
    {
        var error = GetBranchExpressionError("not (path(x) == 1");

        Assert.Contains("position 17", error, StringComparison.Ordinal);
        Assert.Contains("Expected ')'", error, StringComparison.Ordinal);
        Assert.Contains("end of expression", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyConversion_BranchExpressionInvalidInRightHand_ErrorIsActionable()
    {
        var error = GetBranchExpressionError("path(x) in path(y)");

        Assert.Contains("position 18", error, StringComparison.Ordinal);
        Assert.Contains("Right-hand side of 'in' must be an array literal", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyConversion_BranchExpressionTrailingToken_ErrorIsActionable()
    {
        var error = GetBranchExpressionError("path(name) == 'nora')");

        Assert.Contains("Unexpected trailing token ')'", error, StringComparison.Ordinal);
        Assert.Contains("position 20", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseAndFormatQueryString_AreConsistent()
    {
        var (value, error) = ConversionEngine.ParsePayload("user.name=Ada&user.age=37", DataFormat.Query);

        Assert.Null(error);

        var formatted = ConversionEngine.FormatPayload(value, DataFormat.Query, pretty: false);

        Assert.Equal("user.age=37&user.name=Ada", formatted);
    }

    [Fact]
    public void ParsePayload_Stream_ParsesJson()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""{"name":"Ada"}"""));

        var (value, error) = ConversionEngine.ParsePayload(stream, DataFormat.Json);

        Assert.Null(error);
        var output = Assert.IsType<Dictionary<string, object?>>(value);
        Assert.Equal("Ada", output["name"]);
    }

    private static string GetBranchExpressionError(string expression)
    {
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "branch",
                    Expression = expression,
                    Then =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["match"],
                            Source = new ValueSource { Type = "constant", Value = "yes" }
                        }
                    ],
                    Else =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["match"],
                            Source = new ValueSource { Type = "constant", Value = "no" }
                        }
                    ]
                }
            ]
        };

        var result = ConversionEngine.ApplyConversion(
            new Dictionary<string, object?> { ["name"] = "nora", ["x"] = 1d },
            rules);
        Assert.NotEmpty(result.Errors);
        return result.Errors[0];
    }

    private static ConversionResult ConvertWithCollisionRules(ConversionOptions? options = null)
    {
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["name"],
                    Source = new ValueSource { Type = "constant", Value = "first" }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["name"],
                    Source = new ValueSource { Type = "constant", Value = "second" }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["name"],
                    Source = new ValueSource { Type = "constant", Value = "third" }
                }
            ]
        };

        return ConversionEngine.ApplyConversion(new Dictionary<string, object?>(), rules, options);
    }
}
