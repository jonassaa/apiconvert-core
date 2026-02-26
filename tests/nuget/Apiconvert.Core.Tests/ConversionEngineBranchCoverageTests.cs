using System.Text;
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class ConversionEngineBranchCoverageTests
{
    [Fact]
    public async Task StreamConversionAsync_NullStream_ThrowsArgumentNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await foreach (var _ in ConversionEngine.StreamConversionAsync(
                               null!,
                               new ConversionRules { Rules = [] }))
            {
            }
        });
    }

    [Fact]
    public async Task StreamConversionAsync_XmlElements_InvalidXml_ContinueWithReportYieldsStreamError()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<root>"));

        var results = new List<ConversionResult>();
        await foreach (var result in ConversionEngine.StreamConversionAsync(
                           stream,
                           new ConversionRules { Rules = [] },
                           new StreamConversionOptions
                           {
                               InputKind = StreamInputKind.XmlElements,
                               XmlItemPath = "root.item",
                               ErrorMode = StreamErrorMode.ContinueWithReport
                           }))
        {
            results.Add(result);
        }

        Assert.Single(results);
        Assert.Contains(results[0].Errors, e => e.Contains("failed to parse XML stream", StringComparison.Ordinal));
    }

    [Fact]
    public void NormalizeConversionRulesStrict_NullRaw_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ConversionEngine.NormalizeConversionRulesStrict(null));
    }
}
