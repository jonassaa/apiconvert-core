using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using System.Xml.Linq;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class XmlConverterTests
{
    [Fact]
    public void FormatPayload_PrettyXml_UsesIndentedLayout()
    {
        var payload = new Dictionary<string, object?>
        {
            ["root"] = new Dictionary<string, object?>
            {
                ["child"] = "value"
            }
        };

        var prettyOutput = ConversionEngine.FormatPayload(payload, DataFormat.Xml, pretty: true);
        var compactOutput = ConversionEngine.FormatPayload(payload, DataFormat.Xml, pretty: false);

        var prettyDoc = XDocument.Parse(prettyOutput);
        var compactDoc = XDocument.Parse(compactOutput);

        Assert.Equal(
            prettyDoc.ToString(SaveOptions.DisableFormatting),
            compactDoc.ToString(SaveOptions.DisableFormatting));
        Assert.NotEqual(compactOutput, prettyOutput);
    }

    [Fact]
    public void FormatPayload_CompactXml_IsSingleLine()
    {
        var payload = new Dictionary<string, object?>
        {
            ["root"] = new Dictionary<string, object?>
            {
                ["child"] = "value"
            }
        };

        var output = ConversionEngine.FormatPayload(payload, DataFormat.Xml, pretty: false);

        Assert.DoesNotContain(Environment.NewLine, output);
    }
}
