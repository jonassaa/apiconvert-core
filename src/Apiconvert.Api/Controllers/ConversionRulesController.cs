using Apiconvert.Core.Contracts;
using Apiconvert.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apiconvert.Api.Controllers;

[ApiController]
[Route("api/conversion-rules")]
public sealed class ConversionRulesController : ControllerBase
{
    private readonly IConversionRulesGenerator _generator;

    public ConversionRulesController(IConversionRulesGenerator generator)
    {
        _generator = generator;
    }

    [Authorize]
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] ConversionRulesGenerateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.InputSample) || string.IsNullOrWhiteSpace(request.OutputSample))
        {
            return BadRequest(new { error = "Input and output samples are required" });
        }

        var result = await _generator.GenerateAsync(new ConversionRulesGenerationRequest
        {
            InputFormat = request.InputFormat,
            OutputFormat = request.OutputFormat,
            InputSample = request.InputSample,
            OutputSample = request.OutputSample,
            Model = request.Model
        }, cancellationToken);

        return Ok(new { rules = result });
    }
}

public sealed record ConversionRulesGenerateRequest
{
    public DataFormat InputFormat { get; init; } = DataFormat.Json;
    public DataFormat OutputFormat { get; init; } = DataFormat.Json;
    public string InputSample { get; init; } = string.Empty;
    public string OutputSample { get; init; } = string.Empty;
    public string? Model { get; init; }
}
