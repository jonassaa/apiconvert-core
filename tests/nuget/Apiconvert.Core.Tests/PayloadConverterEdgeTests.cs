using System.Text;
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class PayloadConverterEdgeTests
{
    [Fact]
    public void ParsePayload_Stream_ThrowsForNullStream()
    {
        Assert.Throws<ArgumentNullException>(() => ConversionEngine.ParsePayload((Stream)null!, DataFormat.Json));
    }

    [Fact]
    public void ParsePayload_Stream_UsesProvidedEncoding()
    {
        var latin1 = Encoding.Latin1;
        using var stream = new MemoryStream(latin1.GetBytes("{\"name\":\"\u00c5da\"}"));

        var (value, error) = ConversionEngine.ParsePayload(stream, DataFormat.Json, latin1, leaveOpen: true);

        Assert.Null(error);
        var obj = Assert.IsType<Dictionary<string, object?>>(value);
        Assert.Equal("Åda", obj["name"]);
    }

    [Fact]
    public void FormatPayload_Stream_RespectsLeaveOpenFalse()
    {
        var stream = new MemoryStream();

        ConversionEngine.FormatPayload(
            new Dictionary<string, object?> { ["x"] = 1 },
            DataFormat.Json,
            stream,
            pretty: false,
            leaveOpen: false);

        Assert.Throws<ObjectDisposedException>(() => _ = stream.Length);
    }

    [Fact]
    public void ParsePayload_Text_InvalidXmlAndJson_ReturnErrors()
    {
        var (_, xmlError) = ConversionEngine.ParsePayload("<root>", DataFormat.Xml);
        var (_, jsonError) = ConversionEngine.ParsePayload("{", DataFormat.Json);

        Assert.NotNull(xmlError);
        Assert.NotNull(jsonError);
    }
}
