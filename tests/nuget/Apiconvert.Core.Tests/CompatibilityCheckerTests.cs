using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Tests;

public sealed class CompatibilityCheckerTests
{
    [Fact]
    public void CheckCompatibility_ReportsMissingSchemaVersionAsWarning()
    {
        var rules = new ConversionRules
        {
            InputFormat = DataFormat.Json,
            OutputFormat = DataFormat.Json,
            Rules = []
        };

        var report = ConversionEngine.CheckCompatibility(rules, "1.0.0");

        Assert.True(report.IsCompatible);
        Assert.Equal("ACV-COMP-002", report.Diagnostics[0].Code);
    }

    [Fact]
    public void CheckCompatibility_FailsWhenSchemaVersionExceedsTarget()
    {
        var rawRules = """
        {
          "schemaVersion": "1.1.0",
          "inputFormat": "json",
          "outputFormat": "json",
          "rules": []
        }
        """;

        var report = ConversionEngine.CheckCompatibility(rawRules, "1.0.0");

        Assert.False(report.IsCompatible);
        Assert.Contains(report.Diagnostics, diagnostic => diagnostic.Code == "ACV-COMP-004");
    }
}
