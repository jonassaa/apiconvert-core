using Apiconvert.Core.Contracts;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class GenerationModelsTests
{
    [Fact]
    public void ConversionRulesGenerationRequest_HasDefaults()
    {
        var request = new ConversionRulesGenerationRequest();

        Assert.Equal(DataFormat.Json, request.InputFormat);
        Assert.Equal(DataFormat.Json, request.OutputFormat);
        Assert.Equal(string.Empty, request.InputSample);
        Assert.Equal(string.Empty, request.OutputSample);
        Assert.Null(request.Model);
    }

    [Fact]
    public void ConversionRulesGenerationRequest_AllowsCustomValues()
    {
        var request = new ConversionRulesGenerationRequest
        {
            InputFormat = DataFormat.Xml,
            OutputFormat = DataFormat.Query,
            InputSample = "<root />",
            OutputSample = "key=value",
            Model = "gpt-test"
        };

        Assert.Equal(DataFormat.Xml, request.InputFormat);
        Assert.Equal(DataFormat.Query, request.OutputFormat);
        Assert.Equal("<root />", request.InputSample);
        Assert.Equal("key=value", request.OutputSample);
        Assert.Equal("gpt-test", request.Model);
    }
}
