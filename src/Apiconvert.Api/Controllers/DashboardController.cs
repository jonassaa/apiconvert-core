using Apiconvert.Api.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apiconvert.Api.Controllers;

[ApiController]
[Route("api/orgs/{orgId:guid}/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly DashboardService _service;

    public DashboardController(DashboardService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromRoute] Guid orgId, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.GetDashboardAsync(userId.Value, orgId, cancellationToken);
        if (!result.Ok || result.Value == null)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to load dashboard");
        }

        return Ok(new { dashboard = result.Value });
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
            _ => BadRequest(new { error = message })
        };
    }
}
