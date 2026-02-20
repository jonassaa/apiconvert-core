using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Tests;

public sealed class RuleDoctorTests
{
    [Fact]
    public void RunRuleDoctor_ReturnsLintAndRuntimeFindings()
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
                    OutputPaths = ["customer.name"],
                    Source = new ValueSource { Type = "path", Path = "user.name" },
                    DefaultValue = string.Empty
                }
            ]
        };

        var report = ConversionEngine.RunRuleDoctor(
            rules,
            sampleInputText: "{\"user\":{\"name\":\"Ada\"}}",
            inputFormat: DataFormat.Json);

        Assert.NotEmpty(report.Findings);
        Assert.Equal("lint", report.Findings[0].Stage);
        Assert.Equal("ACV-LINT-002", report.Findings[0].Code);
        Assert.False(report.HasErrors);
        Assert.NotEmpty(report.SafeFixPreview);
    }

    [Fact]
    public void RunRuleDoctor_ReportsParseErrorWhenSampleInputIsInvalid()
    {
        var rules = new ConversionRules
        {
            InputFormat = DataFormat.Json,
            OutputFormat = DataFormat.Json,
            Rules = []
        };

        var report = ConversionEngine.RunRuleDoctor(
            rules,
            sampleInputText: "{not-json}",
            inputFormat: DataFormat.Json);

        var parseFinding = Assert.Single(report.Findings, finding => finding.Code == "ACV-DOCTOR-001");
        Assert.Equal(RuleDoctorFindingSeverity.Error, parseFinding.Severity);
        Assert.True(report.HasErrors);
    }
}
