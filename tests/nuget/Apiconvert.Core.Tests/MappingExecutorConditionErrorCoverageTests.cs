using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class MappingExecutorConditionErrorCoverageTests
{
    [Theory]
    [InlineData("path(name) equals 'Ada'", "Did you mean 'eq' or '=='?")]
    [InlineData("path(name) neq 'Ada'", "Did you mean 'not eq' or '!='?")]
    [InlineData("path(name) ge 'Ada'", "Did you mean")]
    [InlineData("path(name) in 'Ada'", "Right-hand side of 'in' must be an array literal")]
    [InlineData("path(name) >", "Expected right-hand operand after operator '>'")]
    [InlineData("exists(path(name)", "Expected ')' but found end of expression")]
    [InlineData("path()", "path(...) requires a path reference")]
    [InlineData("foo eq 1", "Unexpected identifier 'foo'")]
    [InlineData("path(name) + 1", "Unexpected character '+'")]
    [InlineData("path(name) eq 'Ada", "Unterminated string literal")]
    public void ApplyConversion_InvalidBranchExpressions_ReportDeterministicErrors(string expression, string expectedMessage)
    {
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "branch",
                    Expression = expression,
                    Then = []
                }
            ]
        };

        var result = ConversionEngine.ApplyConversion(new Dictionary<string, object?> { ["name"] = "Ada" }, rules);

        Assert.Contains(result.Errors, error => error.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Fact]
    public void ApplyConversion_RawInvalidRules_PromotesValidationErrorToRuntimeDiagnostic()
    {
        var result = ConversionEngine.ApplyConversion(new Dictionary<string, object?>(), "{");

        Assert.Contains(result.Errors, error => error.Contains("invalid JSON payload", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Diagnostics, d => d.Code == "ACV-RUN-000");
    }

    [Fact]
    public void ApplyConversion_MissingOutputPaths_ProducesRun100ForFieldAndArray()
    {
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = [],
                    Source = new ValueSource { Type = "constant", Value = "x" }
                },
                new RuleNode
                {
                    Kind = "array",
                    InputPath = "items",
                    OutputPaths = [],
                    ItemRules = []
                }
            ]
        };

        var result = ConversionEngine.ApplyConversion(new Dictionary<string, object?> { ["items"] = new List<object?>() }, rules);

        Assert.True(result.Diagnostics.Count(d => d.Code == "ACV-RUN-100") >= 2);
    }
}
