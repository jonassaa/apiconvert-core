using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class Final95PushCoverageTests
{
    [Fact]
    public void MappingExecutor_CoversNumericTypeComparisonsAndOperatorEdges()
    {
        var input = new Dictionary<string, object?>
        {
            ["b"] = (byte)1,
            ["sb"] = (sbyte)1,
            ["s"] = (short)1,
            ["us"] = (ushort)1,
            ["i"] = 1,
            ["ui"] = (uint)1,
            ["l"] = (long)1,
            ["ul"] = (ulong)1,
            ["f"] = (float)1,
            ["d"] = 1d,
            ["m"] = 1m,
            ["str"] = "1"
        };

        var rules = new ConversionRules
        {
            Rules =
            [
                Branch("path(b) eq 1", "b"),
                Branch("path(sb) eq 1", "sb"),
                Branch("path(s) eq 1", "s"),
                Branch("path(us) eq 1", "us"),
                Branch("path(i) eq 1", "i"),
                Branch("path(ui) eq 1", "ui"),
                Branch("path(l) eq 1", "l"),
                Branch("path(ul) eq 1", "ul"),
                Branch("path(f) eq 1", "f"),
                Branch("path(d) eq 1", "d"),
                Branch("path(m) eq 1", "m"),
                Branch("path(str) eq 1", "str"),
                new RuleNode { Kind = "branch", Expression = ")", Then = [] },
                new RuleNode { Kind = "branch", Expression = "path(i) not nope", Then = [] },
                new RuleNode { Kind = "branch", Expression = "1..2 eq 1", Then = [] }
            ]
        };

        var result = ConversionEngine.ApplyConversion(input, rules);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);

        Assert.True(output.ContainsKey("b"));
        Assert.True(output.ContainsKey("str"));
        Assert.Equal(3, result.Errors.Count(e => e.Contains("invalid branch expression", StringComparison.Ordinal)));
    }

    [Fact]
    public void MappingExecutor_CoversArrayErrorInvalidPolicyAndResolvePathRootArray()
    {
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "array",
                    InputPath = "value",
                    OutputPaths = ["arr"],
                    ItemRules = []
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["$.rootValue", "$", " "],
                    Source = new ValueSource { Type = "path", Path = "$[0].x" }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["collision"],
                    Source = new ValueSource { Type = "constant", Value = "a" }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["collision"],
                    Source = new ValueSource { Type = "constant", Value = "b" }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["mergeFirst"],
                    Source = new ValueSource
                    {
                        Type = "merge",
                        Paths = ["empty", "name"],
                        MergeMode = MergeMode.FirstNonEmpty
                    }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["mergeArray"],
                    Source = new ValueSource
                    {
                        Type = "merge",
                        Paths = ["name", "empty"],
                        MergeMode = MergeMode.Array
                    }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["splitNull"],
                    Source = new ValueSource { Type = "transform", Transform = TransformType.Split, Path = "missing" }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["splitEmptySep"],
                    Source = new ValueSource { Type = "transform", Transform = TransformType.Split, Path = "name", Separator = "" }
                },
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["conditionMissingExpr"],
                    Source = new ValueSource { Type = "condition", TrueValue = "x" }
                }
            ]
        };

        var input = new Dictionary<string, object?> { ["value"] = "not-array", ["name"] = "Ada", ["empty"] = "" };
        var options = new ConversionOptions { CollisionPolicy = (OutputCollisionPolicy)999 };

        var result = ConversionEngine.ApplyConversion(new List<object?> { new Dictionary<string, object?> { ["x"] = "root" } }, rules, options);

        Assert.Contains(result.Diagnostics, d => d.Code == "ACV-RUN-101" || d.Code == "ACV-RUN-102");
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        Assert.Null(output["rootValue"]);
        Assert.Equal("b", output["collision"]);
        Assert.Null(output["mergeFirst"]);
        Assert.IsType<List<object?>>(output["mergeArray"]);
        Assert.Null(output["splitNull"]);
        Assert.Null(output["splitEmptySep"]);
        Assert.Contains(result.Errors, e => e.Contains("condition expression is required", StringComparison.Ordinal));
    }

    [Fact]
    public void QueryStringConverter_CoversObjectContainerReassignment()
    {
        var (value, error) = ConversionEngine.ParsePayload("a[0]=x&a.name=y", DataFormat.Query);

        Assert.Null(error);
        var obj = Assert.IsType<Dictionary<string, object?>>(value);
        var a = Assert.IsType<Dictionary<string, object?>>(obj["a"]);
        Assert.Equal("y", a["name"]);
    }

    [Fact]
    public void RulesLinter_CoversElseIfTraversalAndNullExpressionLiteralPath()
    {
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "branch",
                    Expression = null,
                    Then =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["x"],
                            Source = new ValueSource { Type = "constant", Value = "1" }
                        }
                    ],
                    ElseIf =
                    [
                        new BranchElseIfRule
                        {
                            Expression = "path(a) eq 1",
                            Then =
                            [
                                new RuleNode
                                {
                                    Kind = "field",
                                    OutputPaths = ["x"],
                                    Source = new ValueSource { Type = "constant", Value = "2" }
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var diagnostics = ConversionEngine.LintRules(rules);
        Assert.Contains(diagnostics, d => d.Code == "ACV-LINT-005");
    }

    [Fact]
    public void RulesNormalizer_CoversNullInputAndWhitespaceOutputPathsAndBranchMergeSelection()
    {
        var empty = ConversionEngine.NormalizeConversionRules(null);
        Assert.Empty(empty.Rules);

        var rules = new ConversionRules
        {
            Fragments = new Dictionary<string, RuleNode>
            {
                ["base"] = new RuleNode
                {
                    Kind = "branch",
                    Expression = "path(a) eq 1",
                    Then =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["$.keep"],
                            Source = new ValueSource { Type = "constant", Value = "x" }
                        }
                    ],
                    ElseIf = [new BranchElseIfRule { Expression = "true", Then = [] }],
                    Else =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["$.elseVal"],
                            Source = new ValueSource { Type = "constant", Value = "y" }
                        }
                    ]
                }
            },
            Rules =
            [
                new RuleNode
                {
                    Use = "base",
                    OutputPaths = ["$. ", "$", "   "],
                    Expression = " ",
                    Then = []
                }
            ]
        };

        var normalized = ConversionEngine.NormalizeConversionRules(rules);
        Assert.NotNull(normalized);
        Assert.NotNull(normalized.Rules);
    }

    private static RuleNode Branch(string expression, string output)
    {
        return new RuleNode
        {
            Kind = "branch",
            Expression = expression,
            Then =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = [output],
                    Source = new ValueSource { Type = "constant", Value = "1" }
                }
            ]
        };
    }
}
