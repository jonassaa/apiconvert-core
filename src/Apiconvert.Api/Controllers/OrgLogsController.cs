using Apiconvert.Api.Logs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apiconvert.Api.Controllers;

[ApiController]
[Route("api/orgs/{orgId:guid}/logs")]
[Authorize]
public sealed class OrgLogsController : ControllerBase
{
    private readonly OrgLogsService _service;

    public OrgLogsController(OrgLogsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromRoute] Guid orgId,
        [FromQuery] Guid? converter,
        [FromQuery] string? q,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        DateTimeOffset? fromValue = ParseDate(from);
        DateTimeOffset? toValue = ParseDate(to);
        var resolvedLimit = limit.HasValue && limit.Value > 0 ? Math.Min(limit.Value, 500) : 200;

        var result = await _service.GetLogsAsync(
            userId.Value,
            orgId,
            converter,
            q,
            fromValue,
            toValue,
            resolvedLimit,
            cancellationToken);

        if (!result.Ok || result.Value == null)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to load logs");
        }

        return Ok(new
        {
            converters = result.Value.Converters,
            logs = result.Value.Logs
        });
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

    private static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }

    private IActionResult MapError(string? code, string message)
    {
        return code switch
        {
            "forbidden" => StatusCode(403, new { error = message }),
            _ => BadRequest(new { error = message })
        };
    }
}
