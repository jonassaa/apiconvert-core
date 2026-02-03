using Apiconvert.Api.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apiconvert.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/orgs/{orgId:guid}/converters/{converterId:guid}/mappings")]
[Authorize]
public sealed class MappingsController : ControllerBase
{
    private readonly ConverterAdminService _service;

    public MappingsController(ConverterAdminService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Save(
        [FromRoute] Guid orgId,
        [FromRoute] Guid converterId,
        [FromBody] MappingSaveDto dto,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var request = new AdminMappingSaveRequest
        {
            OrgId = orgId,
            ConverterId = converterId,
            MappingJson = dto.MappingJson,
            InputSample = dto.InputSample,
            OutputSample = dto.OutputSample
        };

        var result = await _service.SaveMappingAsync(userId.Value, request, cancellationToken);
        if (!result.Ok)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to save mapping");
        }

        return Ok(new { version = result.Value });
    }

    private Guid? ResolveUserId()
    {
        var header = User.FindFirst("sub")?.Value;
        if (Guid.TryParse(header, out var userId))
        {
            return userId;
        }
        return null;
    }

    private IActionResult MapError(string? code, string message)
    {
        return code switch
        {
            "forbidden" => StatusCode(403, new { error = message }),
            "not_found" => NotFound(new { error = message }),
            "org_not_found" => NotFound(new { error = message }),
            _ => BadRequest(new { error = message })
        };
    }
}

public sealed record MappingSaveDto
{
    public string MappingJson { get; init; } = string.Empty;
    public string? InputSample { get; init; }
    public string? OutputSample { get; init; }
}
