using Apiconvert.Api.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apiconvert.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/orgs/{orgId:guid}/converters/{converterId:guid}/logs")]
[Authorize]
public sealed class ConverterLogsController : ControllerBase
{
    private readonly ConverterAdminService _service;

    public ConverterLogsController(ConverterAdminService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromRoute] Guid orgId,
        [FromRoute] Guid converterId,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.GetLogsAsync(userId.Value, orgId, converterId, limit, cancellationToken);
        if (!result.Ok)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to load logs");
        }

        return Ok(result.Value);
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
