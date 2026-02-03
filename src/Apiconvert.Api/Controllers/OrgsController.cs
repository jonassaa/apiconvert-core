using Apiconvert.Api.Organizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apiconvert.Api.Controllers;

[ApiController]
[Route("api/orgs")]
[Authorize]
public sealed class OrgsController : ControllerBase
{
    private readonly OrgService _service;

    public OrgsController(OrgService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.GetOrgListAsync(userId.Value, cancellationToken);
        if (!result.Ok)
        {
            return BadRequest(new { error = result.Error ?? "Failed to load organizations" });
        }

        return Ok(new { orgs = result.Value ?? Array.Empty<OrgListItem>() });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrgCreateDto dto, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.CreateOrgAsync(userId.Value, dto.Name, cancellationToken);
        if (!result.Ok || result.Value == null)
        {
            return BadRequest(new { error = result.Error ?? "Failed to create organization" });
        }

        return Ok(new { org = result.Value });
    }

    [HttpPost("{orgId:guid}/favorites")]
    public async Task<IActionResult> Favorite([FromRoute] Guid orgId, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.SetFavoriteAsync(userId.Value, orgId, true, cancellationToken);
        if (!result.Ok)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to favorite organization");
        }

        return Ok(new { ok = true });
    }

    [HttpDelete("{orgId:guid}/favorites")]
    public async Task<IActionResult> Unfavorite([FromRoute] Guid orgId, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.SetFavoriteAsync(userId.Value, orgId, false, cancellationToken);
        if (!result.Ok)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to unfavorite organization");
        }

        return Ok(new { ok = true });
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

public sealed record OrgCreateDto
{
    public string Name { get; init; } = string.Empty;
}
