using Apiconvert.Api.Organizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apiconvert.Api.Controllers;

[ApiController]
[Route("api/orgs/{orgId:guid}")]
[Authorize]
public sealed class OrgSettingsController : ControllerBase
{
    private readonly OrgSettingsService _service;

    public OrgSettingsController(OrgSettingsService service)
    {
        _service = service;
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings([FromRoute] Guid orgId, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.GetSettingsAsync(userId.Value, orgId, cancellationToken);
        if (!result.Ok || result.Value == null)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to load settings");
        }

        return Ok(new { settings = result.Value });
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateOrg(
        [FromRoute] Guid orgId,
        [FromBody] OrgUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.UpdateOrgNameAsync(userId.Value, orgId, dto.Name, cancellationToken);
        if (!result.Ok)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to update organization");
        }

        return Ok(new { ok = true });
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteOrg([FromRoute] Guid orgId, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.DeleteOrgAsync(userId.Value, orgId, cancellationToken);
        if (!result.Ok)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to delete organization");
        }

        return Ok(new { ok = true });
    }

    [HttpPost("invites")]
    public async Task<IActionResult> CreateInvite(
        [FromRoute] Guid orgId,
        [FromBody] InviteCreateDto dto,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.CreateInviteAsync(userId.Value, orgId, dto.Email, dto.Role, cancellationToken);
        if (!result.Ok || result.Value == null)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to create invite");
        }

        return Ok(new { invite = result.Value });
    }

    [HttpPatch("members/{memberId:guid}")]
    public async Task<IActionResult> UpdateMemberRole(
        [FromRoute] Guid orgId,
        [FromRoute] Guid memberId,
        [FromBody] MemberRoleUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.UpdateMemberRoleAsync(userId.Value, orgId, memberId, dto.Role, cancellationToken);
        if (!result.Ok)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to update member");
        }

        return Ok(new { ok = true });
    }

    [HttpDelete("members/{memberId:guid}")]
    public async Task<IActionResult> RemoveMember(
        [FromRoute] Guid orgId,
        [FromRoute] Guid memberId,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.RemoveMemberAsync(userId.Value, orgId, memberId, cancellationToken);
        if (!result.Ok)
        {
            return MapError(result.ErrorCode, result.Error ?? "Failed to remove member");
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
            "not_found" => NotFound(new { error = message }),
            "org_not_found" => NotFound(new { error = message }),
            _ => BadRequest(new { error = message })
        };
    }
}

public sealed record OrgUpdateDto
{
    public string Name { get; init; } = string.Empty;
}

public sealed record InviteCreateDto
{
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = "member";
}

public sealed record MemberRoleUpdateDto
{
    public string Role { get; init; } = "member";
}
