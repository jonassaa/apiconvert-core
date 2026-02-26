using System.Text;
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class ConversionEngineCoverageTests
{
    [Fact]
    public void CheckCompatibility_ReportsInvalidTargetVersion()
    {
        var report = ConversionEngine.CheckCompatibility(new ConversionRules { Rules = [] }, "not-a-version");

        Assert.False(report.IsCompatible);
        Assert.Contains(report.Diagnostics, entry => entry.Code == "ACV-COMP-001");
    }

    [Fact]
    public void CheckCompatibility_ReportsInvalidSchemaVersion()
    {
        var rules = """
        {
          "schemaVersion": "abc",
          "rules": []
        }
        """;

        var report = ConversionEngine.CheckCompatibility(rules, "1.0.0");

        Assert.False(report.IsCompatible);
        Assert.Contains(report.Diagnostics, entry => entry.Code == "ACV-COMP-005");
    }

    [Fact]
    public void ProfileConversionPlan_ThrowsForNullOrEmptyInputs()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ConversionEngine.ProfileConversionPlan(new ConversionRules { Rules = [] }, null!));

        Assert.Throws<ArgumentException>(() =>
            ConversionEngine.ProfileConversionPlan(new ConversionRules { Rules = [] }, Array.Empty<object?>()));
    }

    [Fact]
    public void ApplyConversion_WithCompiledPlan_ThrowsWhenPlanIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ConversionEngine.ApplyConversion(new Dictionary<string, object?>(), (ConversionPlan)null!));
    }

    [Fact]
    public async Task StreamConversionAsync_XmlElements_RequiresXmlItemPath()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<root><item>1</item></root>"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in ConversionEngine.StreamConversionAsync(
                               stream,
                               new ConversionRules { Rules = [] },
                               new StreamConversionOptions
                               {
                                   InputKind = StreamInputKind.XmlElements,
                                   XmlItemPath = "  "
                               }))
            {
            }
        });

        Assert.Contains("XmlElements streaming requires StreamConversionOptions.XmlItemPath", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamConversionAsync_FailFast_ThrowsOnConversionError()
    {
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "branch",
                    Expression = "path(name) is 'Ada'",
                    Then = []
                }
            ]
        };

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""
            {"name":"Ada"}
            """));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in ConversionEngine.StreamConversionAsync(
                               stream,
                               rules,
                               new StreamConversionOptions
                               {
                                   InputKind = StreamInputKind.Ndjson,
                                   ErrorMode = StreamErrorMode.FailFast
                               }))
            {
            }
        });

        Assert.Contains("conversion failed", error.Message, StringComparison.Ordinal);
        Assert.Contains("stream[0]", error.Message, StringComparison.Ordinal);
    }
}
