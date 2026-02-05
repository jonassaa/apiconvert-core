using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
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

        var output = ConversionEngine.FormatPayload(payload, DataFormat.Xml, pretty: true);

        Assert.Contains(Environment.NewLine, output);
        Assert.Contains($"{Environment.NewLine}  <child>", output);
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
