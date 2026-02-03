using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apiconvert.Api.Controllers;

[ApiController]
[Route("api/conversion")]
public sealed class ConversionController : ControllerBase
{
    [Authorize]
    [HttpPost]
    public IActionResult Convert([FromBody] ConversionRequest request)
    {
        if (request.Rules == null)
        {
            return BadRequest(new ConversionResponse(null, new List<string> { "Rules are required." }));
        }

        var (value, error) = ConversionEngine.ParsePayload(request.Payload ?? string.Empty, request.Rules.InputFormat);
        if (error != null)
        {
            return BadRequest(new ConversionResponse(null, new List<string> { error }));
        }

        var result = ConversionEngine.ApplyConversion(value, request.Rules);
        if (result.Errors.Count > 0)
        {
            return BadRequest(new ConversionResponse(null, result.Errors));
        }

        var formatted = ConversionEngine.FormatPayload(result.Output, request.Rules.OutputFormat, request.Pretty);
        return Ok(new ConversionResponse(formatted, result.Errors));
    }
}

public sealed record ConversionRequest
{
    public string? Payload { get; init; }
    public ConversionRules? Rules { get; init; }
    public bool Pretty { get; init; }
}

public sealed record ConversionResponse(string? Output, List<string> Errors);
