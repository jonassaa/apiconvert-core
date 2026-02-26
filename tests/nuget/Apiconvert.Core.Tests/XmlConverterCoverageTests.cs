using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using System.Globalization;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class XmlConverterCoverageTests
{
    [Fact]
    public void ParsePayload_Xml_ParsesAttributesTextAndRepeatedElements()
    {
        const string xml = """
        <customer id="42">
          <name>Ada</name>
          <tag>a</tag>
          <tag>b</tag>
          <note priority="high">hello</note>
        </customer>
        """;

        var (value, error) = ConversionEngine.ParsePayload(xml, DataFormat.Xml);

        Assert.Null(error);
        var root = Assert.IsType<Dictionary<string, object?>>(value);
        var customer = Assert.IsType<Dictionary<string, object?>>(root["customer"]);

        Assert.Equal("42", Convert.ToString(customer["@_id"], CultureInfo.InvariantCulture));
        Assert.Equal("Ada", customer["name"]);

        var tags = Assert.IsType<List<object?>>(customer["tag"]);
        Assert.Equal("a", tags[0]);
        Assert.Equal("b", tags[1]);

        var note = Assert.IsType<Dictionary<string, object?>>(customer["note"]);
        Assert.Equal("high", note["@_priority"]);
        Assert.Equal("hello", note["#text"]);
    }

    [Fact]
    public void FormatPayload_Xml_CoversFallbackAndCompositeNodes()
    {
        var xmlFromNonObject = ConversionEngine.FormatPayload("x", DataFormat.Xml, pretty: false);
        Assert.Equal("<root />", xmlFromNonObject);

        var xmlFromMultiRoot = ConversionEngine.FormatPayload(
            new Dictionary<string, object?>
            {
                ["a"] = 1,
                ["b"] = new Dictionary<string, object?>
                {
                    ["@_kind"] = "meta",
                    ["#text"] = "hello",
                    ["item"] = new List<object?> { "x", "y" }
                }
            },
            DataFormat.Xml,
            pretty: false);

        Assert.Contains("<root>", xmlFromMultiRoot, StringComparison.Ordinal);
        Assert.Contains("<a>1</a>", xmlFromMultiRoot, StringComparison.Ordinal);
        Assert.Contains("<b kind=\"meta\">", xmlFromMultiRoot, StringComparison.Ordinal);
        Assert.Contains("<item>x</item>", xmlFromMultiRoot, StringComparison.Ordinal);
        Assert.Contains("<item>y</item>", xmlFromMultiRoot, StringComparison.Ordinal);
    }
}
